using [https://github.com/motdotla/dotenv](https://github.com/motdotla/dotenv) to set env variables for connection strings.

Create a .env file to set **process.env.CONNECTION_STRING** and **process.env.EVENTHUB_PATH**

Format for the .env file is:

```
CONNECTION_STRING=Endpoint=sb://blahblahblah.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=234566762354712634sadkfhaskdhgasoQHrg=
EVENTHUB_PATH=demoeventhub
```


or set them via your Docker Config, or however else you want to configure the node image.

Start multiple instances to crank up the event generation rates.

To build the docker image [I used powershell in Windows 10, but you should be able to do the same im any OS](https://nodejs.org/en/docs/guides/nodejs-docker-webapp/):

1. change to the directory where you have cloned the repo.
1. run the following command:  
      **docker build . -t yourtaghere**
1. you will then see the image in your list of images listed by running:  
      **docker images**

To run the docker image:

1. Before we run the docker image, we have to know the connection string and eventhub path for the event hub created by the deployment script.  
We shall be passing those in to the **docker run** command to set the environment variables without needing to hardcode them in our image itself.
1. Once you have the connection string information from your Event Hub in the Azure Portal, run one of the following commands:  
Using the "-e" switch, note the need for the speechmarks to avoid escaping characters in the connection string:  
**docker run -e CONNECTION_STRING="Endpoint=sb://blahblahblah.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=234566762354712634sadkfhaskdhgasoQHrg=" -e EVENTHUB_PATH="demoeventhub" yourtagname**  
or, alternatively, use a simple text file that just contains a list of the env variables, in the same form as above (without speech marks like the .env file mentioned above):  
**docker run --env-file .env yourtaghere**

You can pull the image from DockerHub here:

**docker pull edwinhuber/nodegamingbidemo**

