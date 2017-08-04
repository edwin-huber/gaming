using System;
using System.Collections.Generic;
using System.Linq;

namespace NetCoreGameEventsGenerator
{
    /// <summary>
    ///  This class generates two continuous streams of game events: players starting a game and players stopping a game.
    ///  Data moved from csv files to static string arrays.
    /// </summary>
    public class GameDataEventGenerator
    {
        // Various games
        private const int MaxGameId = 5;
        // maximum session length of 300 seconds
        private const int MaxSessionLength = 300;
        private const double CrashProbability = 0.1;

        private static List<Player> Players;
        private static List<Location> WorldCities;
        private static int numPlayers;
        private static int numWorldCities;

        private static bool BackgroundMode;

        private readonly Random random;
        private readonly EventBuffer eventBuffer;

        public GameDataEventGenerator(Random r)
        {
            random = r;
            eventBuffer = new EventBuffer();
            Players = new List<Player>();
            WorldCities = new List<Location>();

            BackgroundMode = true;

            InitializePlayers();
        }

        /// <summary>
        /// Creates the list of world cities and players with their corresponding locations
        /// </summary>
        public void InitializePlayers()
        {
            // var reader = File.ReadAllLines(@".\Data\world_cities.csv");
            for (int i = 0; i < Data.WorldCities.Cities.GetUpperBound(0); i++)
            {
                WorldCities.Add(new Location(Data.WorldCities.Cities[i,0],
                                                Data.WorldCities.Cities[i, 2], 
                                                Data.WorldCities.Cities[i, 3], 
                                                Data.WorldCities.Cities[i, 1]));
            }
            numWorldCities = WorldCities.Count;
            int coordIdx = 0;

            if (BackgroundMode)
            {
                numPlayers = Data.GamersRest.Gamers.Length; 

                foreach (var player in Data.GamersRest.Gamers)
                {
                    coordIdx = random.Next(numWorldCities - 1);
                    Players.Add(new Player(player, WorldCities.ElementAt(coordIdx)));
                }
            }
            else
            {
                // Players from Germany (GDC taking place in Cologne, Germany)
                List<Location> GermanCities = WorldCities.Where(loc => loc.Country.Equals("Germany")).ToList();
                GermanCities.AddRange(WorldCities.Where(loc => loc.Country.Equals("Poland")));
                Location Cologne = WorldCities.Where(loc => loc.City == "Cologne").First();
                int numGermanCities = GermanCities.Count;
                // percentage of players coming from Cologne
                int numColognePlayers = (int)numPlayers / 20;

                for (int i = 0; i < Data.GermanGamers.Gamers.Length; i++)
                {
                    if (i < numColognePlayers)
                    {
                        Players.Add(new Player(Data.GermanGamers.Gamers[i], Cologne));
                    }
                    else
                    {
                        coordIdx = random.Next(numGermanCities - 1);
                        Players.Add(new Player(Data.GermanGamers.Gamers[i], GermanCities.ElementAt(coordIdx)));
                    }
                }

                // Players from Brazil (GDC taking place during Olympic Games in Brazil)
                List<Location> BrazilianCities = WorldCities.Where(loc => loc.Country.Equals("Brazil")).ToList();
                Location Rio = WorldCities.Where(loc => loc.City.Equals("Rio de Janeiro")).First();
                int numBrazilianCities = BrazilianCities.Count;
                // percentage of players in Rio de Janeiro
                int numRioPlayers = (int)numPlayers / 5;

                for (int i = 0; i < Data.BrazilianGamers.Gamers.Length; i++)
                {
                    if (i < numRioPlayers)
                    {
                        Players.Add(new Player(Data.BrazilianGamers.Gamers[i], Rio));
                    }
                    else
                    {
                        coordIdx = random.Next(numBrazilianCities - 1);
                        Players.Add(new Player(Data.BrazilianGamers.Gamers[i], BrazilianCities.ElementAt(coordIdx)));
                    }
                }

            }
            
            numPlayers = Players.Count;
        }

        public static GameDataEventGenerator Generator()
        {
            return new GameDataEventGenerator(new Random());
        }

        /// <summary>
        /// Generates n game events within a given interval starting at startTime
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="interval"></param>
        /// <param name="n">Number of game events</param>
        public void Next(DateTime startTime, TimeSpan interval, int n)
        {
            for (int i = 0; i < n; i++)
            {
                //var playerId = PlayerIds[random.Next(PlayerIds.Length)];
                var player = Players.ElementAt(random.Next(numPlayers - 1));
                var playerId = player.Name;
                Location playerLoc = player.PlayerLocation;

                var entryTime = startTime + TimeSpan.FromMilliseconds(random.Next((int)interval.TotalMilliseconds));
                var exitTime = entryTime + TimeSpan.FromSeconds(random.Next(MaxSessionLength));
                var crash = random.NextDouble();

                int gameId = 0;
                if (BackgroundMode)
                {
                    gameId = random.Next(MaxGameId);
                }
                if (!BackgroundMode)
                {
                    var gameProb = random.NextDouble();
                    if (gameProb > 0.7)
                    {
                        gameId = random.Next(MaxGameId - 1);
                    }
                    else
                    {
                        gameId = MaxGameId - 1;
                    }
                }

                // Only add GameEvent if there is not already an ExitEvent of given PlayerId with a timestamp greater than given timestamp
                if (!eventBuffer.HasExistingLaterExitEvent(entryTime, playerId))
                {
                    eventBuffer.Add(entryTime, new EntryEvent(playerId, gameId, entryTime, playerLoc));
                    //eventBuffer.Add(entryTime, new EntryEvent(playerId, gameId, entryTime, playerLoc.Latitude, playerLoc.Longitude));

                    if (crash > CrashProbability)
                    {
                        eventBuffer.Add(exitTime, new ExitEvent(playerId, gameId, exitTime, playerLoc));
                        //eventBuffer.Add(exitTime, new ExitEvent(playerId, gameId, exitTime, playerLoc.Latitude, playerLoc.Longitude));
                    }
                }
            }
        }

        public IEnumerable<GameEvent> GetEvents(DateTime startTime)
        {
            return eventBuffer.GetEvents(startTime);
        }
    }
}
