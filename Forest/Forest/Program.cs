using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;

namespace Forest
{
    enum LocationID
    {
        Nowhere,
        Inventory,
        Den,
        Forest
    }

    enum Direction
    {
        North,
        South,
        Weast,
        East
    }

    class LocationData
    {
        public LocationID ID;
        public string Name;
        public string Description;
        public Dictionary<Direction, LocationID> Directions;
    }

    class Program
    {
        const ConsoleColor NarrativeColor = ConsoleColor.Gray;
        const ConsoleColor PromptColor = ConsoleColor.White;
        const int PrintPauseMilliseconds = 150;

        // Data dictionaries
        static Dictionary<LocationID, LocationData> LocationsData = new Dictionary<LocationID, LocationData>();

        // Current state
        static LocationID CurrentLocationID = LocationID.Den;


        static bool quitGame = false;

        static void Print(string text)
        {
            // Split text into lines that don't exceed the window width.
            int maximumLineLength = Console.WindowWidth - 1;
            MatchCollection lineMatches = Regex.Matches(text, @"(.{1," + maximumLineLength + @"})(?:\s|$)");

            // Output each line with a small delay.
            foreach (Match line in lineMatches)
            {
                Console.WriteLine(line.Groups[0].Value);
                Thread.Sleep(PrintPauseMilliseconds);
            }
        }

        static void Reply(string text)
        {
            Print(text);
            Console.WriteLine();
        }

        static void HandlePlayerAction()
        {
            // Ask player what they want to do.
            Console.ForegroundColor = NarrativeColor;
            Print("Whats next?");

            Console.ForegroundColor = PromptColor;
            Console.Write("> ");

            string command = Console.ReadLine().ToLowerInvariant();

            // Split the command into words.
            string[] words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Assuming the first word in the command is a verb. If there is no entered words the verb string stays empty.
            string verb = "";
            if (command != "")
            {
                verb = words[0];
            }

            // Call the right handler for the given verb.
            switch (verb)
            {
                case "north":
                case "n":
                    // TODO
                    break;
                case "south":
                case "s":
                    // TODO
                    break;
                case "west":
                case "w":
                    // TODO
                    break;
                case "east":
                case "e":
                    // TODO
                    break;

                case "take":
                case "pick":
                    //TODO
                    break;
                case "give":
                    //TODO
                    break;
                case "drop":
                    //TODO
                    break;
                case "combine":
                    //TODO
                    break;
                case "inventory":
                case "i":
                    // TODO show list if whats in the inventory
                    break;
                case "talk":
                    //TODO
                    break;
                case "look":
                    //TODO
                    break;
                case "shift":
                    //TODO need to figure out what command I want to use for this
                    break;
                case "sleep":
                    //TODO
                    break;
                case "read":
                    //TODO
                    break;

                // TODO interacting verbs, probably need more of them
                case "eat":
                    //TODO
                    break;


                case "quit":
                case "q":
                case "end":
                case "exit":
                    // TODO ask if the player really wants to quit and if they want to save
                    {
                        Reply("Thanks for playing!");
                        quitGame = true;
                    }
                    break;
                case "save":
                    //TODO
                    break;
                case "load":
                    //TODO
                    break;

                default:
                    // TODO
                    Reply("I don't understand");
                    break;
            }
        }

        static void DisplayLocation()
        {
            Console.Clear();

            // Display current location description.
            LocationData currentLocationData = LocationsData[CurrentLocationID];
            Print(currentLocationData.Description);
        }

        static void Main(string[] args)
        {
            // Initialization

            // Reading title art information
            string titleArtPath = "ForestTitleArt.txt";
            string[] title = File.ReadAllLines(titleArtPath);

            // Reading all the text for the games story
            string storyPath = "ForestGameStory.txt";
            string[] gameStory = File.ReadAllLines(storyPath);

            // Reading location data
            string locationDataPath = "ForestLocations.txt";
            string[] locationData = File.ReadAllLines(locationDataPath);

            // Creating location objects
            string[] locationNames = Enum.GetNames(typeof(LocationID));
            for (var line = 0; line < locationData.Length - 4; line++)
            {
                var locationEntry = new LocationData();

                string locationIDText = locationData[line];
                LocationID location = Enum.Parse<LocationID>(locationIDText);

                locationEntry.ID = location;
                locationEntry.Name = locationData[line + 1];
                locationEntry.Description = locationData[line + 2];

                LocationsData.Add(location, locationEntry);

                line += 3;
            }

            // Displaying title art
            foreach (string line in title)
            {
                Console.WriteLine(line);
            }
            Console.ReadKey();
            Console.Clear();

            // Displaying the introduction/first part of the games story
            Console.ForegroundColor = NarrativeColor;
            Print(gameStory[0]);
            Console.WriteLine();

            // Display short instructions about how to play??

            Console.ReadKey();
            DisplayLocation();

            // Game loop
            while (!quitGame)
            {
                // Ask player what they want to do.
                HandlePlayerAction();
            }
        }
    }
}
