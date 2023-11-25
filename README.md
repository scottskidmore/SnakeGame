# Project Title

Snake Game Client -PS8

## Description

Creates the client-side functionality of a server-based snake game. This client-side program is used to deserialize all commands given by the server, 
display these in a MAUI GUI and return various commands such as player name and direction chosen.

## Getting Started

### Dependencies 11/25/2023

* .NET Maui and Maui Toolkit
* Network Controller DLL given in files

### Installing 10/23/2023

* Install or run the Snake Game server if playing on a local server
* Install the Snake Game solution and set Snake Game Client as the start-up project and run in Visual Studio
* Works on both Mac and Windows

### Executing program  11/25/2023

* Press run on the Snake Game Client project to initialize the Snake CLient MAUI program
* Once the Maui program is successfully launched, enter the desired address of the Snake Game server into the server textbox (if playing a local game skip this step and leave it as is)
* Enter a valid player name of 16 characters or less into the name text box.
* Press the connect button and the game will begin.
* To control movement use the WASD controls, to increase score and length collect the green and orange power-ups, and lastly avoid all other players, walls, and your own tail or you will die and your score will be reset.


### Additional Features as of 11/25/2023
* EXPLOSIONS! whenever anyone dies an explosion effect will appear where they died and dynamically grow and expand for 60 whole frames!
* A Top Score Tracker at the top of the screen, see who has the current top score within the server so you know who to target NEXT!
* Dynamically chosen color, 10 different vibrant colors are chosen for YOUR snake  when you enter the server! Don't like your color? simply reconnect and a new color will be chosen for YOU!
* FUTURE UPDATES will include: ADITIONAL colors to customize your snake further; A Top Three Score Tracker, don't just see the top snake but the top THREE snakes in the server;
* and last but not least, ALL-TIME HIGH SCORES will be recorded in perpetuity on a dedicated web server!


### Help 11/25/2023
* More info on the help button at the top of the Snake Client
* all potential errors and exceptions are handled dynamically through display boxes or errors placed DIRECTLY through the GUI handled by our Network Controller!


## Authors 11/25/2023


Scott Skidmore and
Austin Allen

