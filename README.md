# Project Title

Spreadsheet PS6

## Description

Implementations of the Abstract Spreadsheet to back a spreadsheet gui

## Getting Started

### Dependencies

* .NET Maui and Maui Toolkit
* Windows 10

### Installing

* Instal the spreadsheet project and set the SpreadsheetGUI as the start up project and run
* Must be run on a windows machine Mac causes errors with the Gui

### Executing program

* Just run the project
* Additionally Because of the way that the maui toolkit works with async methods for the load file picker
we were unable to get the load button to open the file and then foreach loop through
each cell to add it to the drawing because of the async method. Using Tasks we were able to get
it to work about 90% of the time but when it did not work it would drop values from the
backing spreadsheet so that reloading still would not add everything. Due to this problem we
decide to load the sheet using the tool kit and then load it into the canvas using the reload
button.

### Help

* Must be run on a windows machine Mac causes errors with the Gui
* More info on the help tab of the spreadsheet gui


## Authors


Scott Skidmore and
Austin Allen
