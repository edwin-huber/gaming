using [https://github.com/motdotla/dotenv](https://github.com/motdotla/dotenv) to set env variables for connection strings.

Create a .env file to set **process.env.CONNECTION_STRING** and **process.env.EVENTHUB_PATH**

or set them via your Docker Config, or however else you want to configure the node image.

Start multiple instances to crank up the event generation rates.