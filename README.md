# AUVSI_SUAS_TargetUpload
The AUVSI_SUAS_TargetUpload application allows users to upload ODLC images and characteristics to the judges server for the AUVSI Student Unammed Aerial Systems Competition: https://www.auvsi-suas.org.

The program is written in C# and uses WPF for the UI. It was adapted from the Mission Planner Interop Plugin that I wrote back in 2018.
The API reference and .proto can be found on the AUVSI Interop github: https://github.com/auvsi-suas/interop. 

# Work in Progress
This is still a work in progress. The core functionality of the program works, but there are a number of features that still need to be implemented before I would consider this program finished. If using this program for competition, please test it with your own server first before you get to the flight line.

# Building and Setup
Clone the repository, build, then run the application. 

The user credentials and server address are stored under Properties -> Settings.settings 
You will have to change the server address to the one you are using.

# Usage
On program startup, click the Connect button, which will initiate a connection to the server. The program will then download any ODLC targets already uploaded to the server. 

Any changes made on the program will only modify a local copy of the targets.  
In order to send your changes to the server, you will need to click the sync button.    

The program does not do any input validation, so if you enter an invalid GPS coordinate, or send an invalid character in alphanumeric field the server will reject the upload. Unfortunatly, the server just sends back a "bad request" message, so make sure you input all the values correctly. 

# Credits
To Be Updated 

# License
To Be Updated 


