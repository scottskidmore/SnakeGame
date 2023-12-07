# Project Title

Snake Game Server - PS9

## Description 12/07/2023

Creates the server-side functionality for the server-based, multiplayer, snake game. This server generates all code for creating, maintaining, and updating the world associated with gameplay. The server will allow for numerous connections from players, take movement-based commands from the clients, and update the world dynamically. There are two game modes as described below.


## Getting Started 

### Dependencies 12/07/2023

* .NET Maui and Maui Toolkit
* Network Controller DLL given in files
* Server settings XML file held in two locations.

### Installing 12/07/2023

* Works on both Mac and Windows
* Install or run the Snake Game client, or run the game with our given client(cannot guarantee stability with our client)
* Install the Snake Game solution, set our server as the start-up project, and run it in Visual Studio
* We have provided two identical settings file locations the main location where the settings file MUST exist is
  inside this file path:  "game-dreamweavers_game/Server/bin/Debug/net7.0/settings.xml". There is a secondary identical backup file located at this 
  file path: "game-dreamweavers_game/Settings/settings.xml" This file is given in case the main file within the net7.0 is ever accidentally deleted, 
  corrupted, or was not included. If you ever need to replace the net7.0 file simply copy the backup file and paste it into the main file location.
  
  


### Executing program  12/07/2023

* Once the settings XML file has been verified or replaced, start the server in Visual Studio or use the server executable within the downloaded files.
* Run your Snake Game Client or the one we provided.
* Once the Snake Game Client Maui program is successfully launched, enter the desired address of the Snake Game server into the server textbox (if playing a local game skip this step and leave it as is)
* Enter a valid player name of 16 characters or less into the name text box.
* Press the connect button and the game will begin.
* To control movement use the WASD controls, to increase score and length collect the green and orange power-ups, and lastly avoid all other players, walls, and your own tail or you will die and your score will be reset.

## Game Mechanics

### Our Working Design Choices
* Our snakes move correctly when

### Our Design Choices That Do Not Work Correctly
* Snakes can spawn on powerups and vice versa; we attempted to remedy this by adding a second check method but it would have required mass changes to 
  our code and we did not realize this could happen until the last day.
* Snakes can 180 on themselves if they turn quickly enough, although they do not die, the same issue as above.
* Settings file location, we wanted to be able to call our settings file from a different location other than the default net7.0 without declaring a 
  full file path so that no changes or checks needed to be made before running on a new computer. This did not work even with various file location 
  methods from both Path. and System.
* Smaller world size, We wanted to have the deathmatch be a smaller map by reducing the size of the world size so that only the inner middle square 
  was drawn. This did not work because we could not dynamically remove the walls that were outside of the new world size without causing significant 
  changes to how the world walls were stored.
  

### New Game Mode: DeathMatch
* There is a game mode that can be accessed by opening up the MAIN settings file, located within the file path given in the installation section, and changing <DeathMatch>false</DeathMatch> to <DeathMatch>true</DeathMatch>. and run the server.
  
* This game mode COMPLETELY changes the functionality of the server for a brand NEW experience by implementing the following changes:
              * Score increases when you "kill" another snake by having them run into you
              * Score does not reset when you die, so go out there and eliminate as many snakes as possible
              * PowerUps, instead of growing your snake, make you invincible for 5 seconds so that you are invulnerable to other snakes (Invincibility 
                does not protect you from walls)                                                                                 
              * PowerUps now spawn more infrequently so you must FIGHT other players for them!
* Future updates to this game mode will include a smaller map for more dynamic play, a leaderboard for tracking this information, and a timer for     
  deathmatches so that winners can be declared.


### Help 11/25/2023
* More info on the help button at the top of the Snake Client
* all potential errors and exceptions are handled dynamically through display boxes or errors placed DIRECTLY through the GUI handled by our Network Controller!


## Authors 11/25/2023


Scott Skidmore and
Austin Allen

