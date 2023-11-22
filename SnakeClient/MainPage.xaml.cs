﻿

using GameController;
using System.Text.Json;
using World;

namespace SnakeGame;

public partial class MainPage : ContentPage
{
    Controller gameController = new();
    private string moving;
    private bool canMove = false;
    public MainPage()
    {
        InitializeComponent();
        gameController.Error += ShowError;
        gameController.NewUpdate+=OnFrame;
        worldPanel.SetWorld(gameController.GetWorld());
        graphicsView.Invalidate();
    }

    /// <summary>
    /// Handler for the controller's Error event
    /// </summary>
    /// <param name="err"></param>
    private void ShowError(string err)
    {
        // Show the error
        Dispatcher.Dispatch(() => DisplayAlert("Error", err, "OK"));

        // Then re-enable the controls so the user can reconnect
        Dispatcher.Dispatch(
          () =>
          {
              connectButton.IsEnabled = true;
              serverText.IsEnabled = true;
          });
    }


    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();

        if (canMove == false) 
        {
            entry.Text = "";
            return;

        }

        
        if (text == "w")
        {
            // Move up
            moving = "up";
        }
        else if (text == "a")
        {
            // Move left
            moving = "left";
        }
        else if (text == "s")
        {
            // Move down
            moving = "down";
        }
        else if (text == "d")
        {
            // Move right
            moving = "right";
        }
        //if a moving command was entered
        if (moving != null)
        {
            //string jsonString = JsonSerializer.Serialize(moving);
            gameController.MessageEntered(moving);
            //reset moving text
            moving = null;
            //disable entry until next frame
            canMove = false;
        }
        //reset entry text
        entry.Text = "";
        
    }

    private void NetworkErrorHandler()
    {
        DisplayAlert("Error", "Disconnected from server", "OK");
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
        // Disable the controls and try to connect
        connectButton.IsEnabled = false;
        serverText.IsEnabled = false;
        
        //Send info to game controller
        gameController.Connect(serverText.Text, nameText.Text);
        //what does this do?
        keyboardHack.Focus();
    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
        //reenable controls
        canMove = true;
    }

    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by ...\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}