// Globals and requires
// ############
const config = require('dotenv').config();
const Gamers = require('./Data/GamersRest.js').Gamers;
const Cities = require('./Data/WorldCities.js').Cities;
const EventHubClient = require('azure-event-hubs').Client;

const intervalMilliseconds = 2000;
const totalIterations = 10000;
const MaxGameId = 5;

const MaxSessionLength = 300;
const exitProbability = 0.1;

var Players = [];
var WorldCities = []; 
var numPlayers;
var numWorldCities;

// The Event Hubs SDK can also be used with an Azure IoT Hub connection string.
// In that case, the eventHubPath variable is not used and can be left undefined.
var connectionString = process.env.CONNECTION_STRING; 
var eventHubPath = process.env.EVENTHUB_PATH ;

// Game Data Classes
//###############

class GameLocation {
    constructor(Latitude, Longitude, City, Country) {
        this.Latitude = Latitude;
        this.Longitude = Longitude;
        this.City = City;
        this.Country = Country;
    }
}

class GameEventKey {
    constructor(TimeStamp, EventId) {
        this.TimeStamp = TimeStamp;
        this.EventId = EventId;
    }
}

class GameEvent {
    constructor(PlayerId, GameId, PlayerLocation) {
        this.PlayerId = PlayerId;
        this.GameId = GameId;
        this.PlayerLocation = PlayerLocation;
    }
    Format() {
        return JSON.stringify(this);
    };
}

class EntryEvent {
    constructor(EntryTime, PlayerId, GameId, PlayerLocation) {
        this.EntryTime = EntryTime;
        this.PlayerId = PlayerId;
        this.GameId = GameId;
        this.PlayerLocation = PlayerLocation;
    }
    Format() {
        return this.FormatJson();
    }

    FormatJson() {
        var jsonObj = {
            PlayerId: this.PlayerId,
            GameId: this.GameId,
            Time: this.EntryTime,
            GameActivity: "1",
            Latitude: this.PlayerLocation.Latitude,
            Longitude: this.PlayerLocation.Longitude,
            City: this.PlayerLocation.City,
            Country: this.PlayerLocation.Country
        };
        return JSON.stringify(jsonObj);
    }

}

class ExitEvent {
    constructor(ExitTime, PlayerId, GameId, PlayerLocation) {
        this.ExitTime = ExitTime;
        this.PlayerId = PlayerId;
        this.GameId = GameId;
        this.PlayerLocation = PlayerLocation;
    }
    Format() {
        return this.FormatJson();
    }

    FormatJson() {
        var jsonObj = {

            PlayerId: this.PlayerId,
            GameId: this.GameId,
            Time: this.ExitTime,
            GameActivity: "0",
            Latitude: this.PlayerLocation.Latitude,
            Longitude: this.PlayerLocation.Longitude,
            City: this.PlayerLocation.City,
            Country: this.PlayerLocation.Country
        };
        return JSON.stringify(jsonObj);
    }

}

class EventBuffer {
    constructor() {
        this.eventId = 0;
        this.events = [];
    }
    Add(time, e) {
        this.events.push(e);
        console.log("Added to eventBuffer: %s, eID:%s, %s, %s", time, this.eventId, e.PlayerId, e.GameId);
        this.eventId++;
    }
}

var eventBuffer = new EventBuffer(); // class is not hoisted so needs init here...

// Event generation and Data Prep
//###############
function next(startTime, interval, numEvents) {

    for (i = 0; i < numEvents; i++) {

        let player = Players[(Math.floor(Math.random() * Players.length))];
        let playerId = player.Name;
        let playerLoc = player.PlayerLocation;

        let entryTime = startTime; 

        let diff = startTime + Math.floor(Math.random() * MaxSessionLength);

        let exitTime = new Date(diff);
        let willExit = Math.random();

        let gameId = Math.floor(Math.random() * MaxGameId);

        // Original code would Only add GameEvent if there is not already an ExitEvent of given PlayerId with a timestamp greater than given timestamp
        // not sure if that is really necessary for our demo
        eventBuffer.Add(entryTime, new EntryEvent(entryTime, playerId, gameId, playerLoc));

        if (willExit > exitProbability) {
            eventBuffer.Add(exitTime, new ExitEvent(exitTime, playerId, gameId, playerLoc));
        }
    }
}

function initializePlayers() {
    for (i = 0; i < Cities.length; i++) {
        // Our array format looks like: ["Abu Dhabi","United Arab Emirates","24.46667","54.36667"],
        WorldCities.push(new GameLocation(Cities[i][3], Cities[i][2], Cities[i][0], Cities[i][1]));
    }
    numWorldCities = WorldCities.length;

    Gamers.forEach((player) => {
        let coordIdx = Math.floor(Math.random() * (numWorldCities - 1));
        Players.push({ Name: player, PlayerLocation: WorldCities[coordIdx] });
    });

    numPlayers = Players.length;
}

function getEvents() {
    var result = [];
    while (eventBuffer.events.length > 0) {
        result.push(eventBuffer.events.pop());
    }
    return result;
}

function printError(err) {
    console.error(err.message);
};

function sendEvent(eventBody) {
        console.log('Sending Event: ' + JSON.stringify(eventBody, undefined, 2));
        sender.then((tx) => tx.send(eventBody))
        .catch(printError);
};

// this is a useful construct for our game loop, as we are using node which is actually
// based on a non blocking event loop, so we need some form of delaying timer loop
// https://www.thecodeship.com/web-development/alternative-to-javascript-evil-setinterval/
function interval(func, wait, times) {
    var interv = function (w, t) {
        return function () {
            if (typeof t === "undefined" || t-- > 0) {
                setTimeout(interv, w);
                try {
                    func.call(null);
                }
                catch (e) {
                    t = 0;
                    throw e.toString();
                }
            }
        };
    }(wait, times);

    setTimeout(interv, wait);
}

// Main Program Code
// ############

let sendInterval = intervalMilliseconds;
let iterations = totalIterations;
var counter = 0;

var client = EventHubClient.fromConnectionString(connectionString, eventHubPath);
var cansend = false;

var sender = client.open()
    .then(function () {
        client.createSender();
    })
    .then(function() {
        cansend = true;
        return client.createSender();
      })
    .catch(printError);

function sendDataToAzure(data) {
    data.forEach((d) => sendEvent(d.FormatJson()));
}

initializePlayers();

// Start game loop
interval(function () {
    counter++;
    next(new Date(), 1, 2);
    if (cansend) {
        sendDataToAzure(getEvents());
        console.log('%s, events sent, iteration %s', new Date(), counter);
    }
}, sendInterval, iterations);

