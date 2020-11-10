using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;

namespace Forest
{
    enum LocationId
    {
        Nowhere,
        Inventory,
        Den,
        Forest,
        Forest2,
        Forest3,
        River,
        Placeholder
    }

    enum ThingId
    {
        Moss,
        Grass,
        Leaves,
        Berries,
        Beehive,
        Fish,
        Necklace,
        Owl,
        Frog
    }

    enum Direction
    {
        North,
        South,
        West,
        East
    }

    class LocationData
    {
        public LocationId Id;
        public string Name;
        public string Description;
        public Dictionary<Direction, LocationId> Directions;
    }

    class ThingData
    {
        public ThingId Id;
        public string Name;
        public string Description;
    }

    class ParsedData
    {
        public string Id;
        public string Name;
        public string Description;
        public Dictionary<Direction, LocationId> Directions;
    }

    class Program
    {
        const ConsoleColor NarrativeColor = ConsoleColor.Gray;
        const ConsoleColor PromptColor = ConsoleColor.White;
        const int PrintPauseMilliseconds = 150;

        // Data dictionaries
        static Dictionary<LocationId, LocationData> LocationsData = new Dictionary<LocationId, LocationData>();

        // Current state
        static LocationId CurrentLocationId = LocationId.Den;


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
                // Directions.
                case "north":
                case "n":
                    HandleMovement(Direction.North);
                    break;
                case "south":
                case "s":
                    HandleMovement(Direction.South);
                    break;
                case "west":
                case "w":
                    HandleMovement(Direction.West);
                    break;
                case "east":
                case "e":
                    HandleMovement(Direction.East);
                    break;

                // Verbs.
                case "take":
                case "pick":
                    // TODO
                    break;
                case "give":
                    // TODO
                    break;
                case "drop":
                    // TODO
                    break;
                case "combine":
                    // TODO
                    break;
                case "talk":
                    // TODO
                    break;
                case "look":
                    // TODO
                    break;
                case "sleep":
                    // TODO
                    break;
                case "read":
                    // TODO
                    break;
                // TODO interacting verbs, probably need more of them
                case "eat":
                    // TODO
                    break;

                // Inventory.
                case "inventory":
                case "i":
                    // TODO show list if whats in the inventory
                    break;

                // Shapeshifting.
                case "shift":
                    // TODO need to figure out what command I want to use for this
                    break;

                // Quit.
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

                // Save and load.
                case "save":
                    // TODO
                    break;
                case "load":
                    // TODO
                    break;

                // Unvalid verb.
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
            LocationData currentLocationData = LocationsData[CurrentLocationId];
            Print(currentLocationData.Description);

            // Array with strings of directions
            string[] allDirections = Enum.GetNames(typeof(Direction));

            // Going through all the directions to se if the current locations contains a location in that direction, and displaying existing directions
            for (int direction = 0; direction < allDirections.Length; direction++)
            {
                Direction currentDirection = Enum.Parse<Direction>(allDirections[direction]);
                if (currentLocationData.Directions.ContainsKey(currentDirection))
                {
                    Print($"{allDirections[direction]}: {currentLocationData.Directions[currentDirection].ToString()}");
                }
            }
        }

        static void HandleMovement(Direction direction)
        {
            LocationData currentLocation = LocationsData[CurrentLocationId];

            // Checking if the direction is availible for the current location.
            if (currentLocation.Directions.ContainsKey(direction))
            {
                // Changing the current location to the new location and displaying the new location information.
                LocationId newLocation = currentLocation.Directions[direction];
                CurrentLocationId = newLocation;
                DisplayLocation();
            }
            else
            {
                // If the player tries to go in a direction with no location.
                Reply("That direction is not possible.");
            }
        }

        static void Main(string[] args)
        {
            // Initialization

            // Reading title art information
            string[] titleAsciiArt = File.ReadAllLines("ForestTitleArt.txt");

            // Reading all the text for the games story
            string[] gameStory = File.ReadAllLines("ForestGameStory.txt");

            // Reading location data
            string[] locationData = File.ReadAllLines("ForestLocations.txt");

            // Creating location objects
            string[] locationNames = Enum.GetNames(typeof(LocationId));

            // Reading thing data
            string[] thingData = File.ReadAllLines("ForestThings.txt");

            // Creating location objects
            string[] thingNames = Enum.GetNames(typeof(ThingId));



            bool newEntry = true;
            var locationEntry = new LocationData();
            LocationId locationEntryId = LocationId.Placeholder;
            for (int line = 0; line < locationData.Length; line++)
            {
                if (newEntry)
                {
                    locationEntry = new LocationData();
                    locationEntry.Directions = new Dictionary<Direction, LocationId>();
                    locationEntryId = LocationId.Placeholder;
                    newEntry = false;
                }

                Match match = Regex.Match(locationData[line], "^([A-Z].*): ?(.*)");

                string property = match.Groups[1].Value;
                string value = match.Groups[2]?.Value;

                switch (property)
                {
                    case "ID":
                        locationEntryId = Enum.Parse<LocationId>(value);
                        locationEntry.Id = locationEntryId;
                        break;

                    case "Name":
                        locationEntry.Name = value;
                        break;

                    case "Description":
                        locationEntry.Description = value;
                        break;

                    case "Directions":
                        do
                        {
                            Match directionAndDestination = Regex.Match(locationData[line + 1], @"[ \t]*([A-Z]\w*): (\w*)");

                            if (directionAndDestination.Value == "")
                            {
                                break;
                            }

                            Direction direction = Enum.Parse<Direction>(directionAndDestination.Groups[1].Value);
                            LocationId destination = Enum.Parse<LocationId>(directionAndDestination.Groups[2].Value);

                            locationEntry.Directions[direction] = destination;

                            line++;

                        } while (line + 1 < locationData.Length);

                        break;
                    case "":
                        LocationsData.Add(locationEntryId, locationEntry);
                        newEntry = true;
                        break;
                }
            }

            /*for (var line = 0; line < locationData.Length - 4; line++)
            {
                var locationEntry = new LocationData();

                string locationIDText = locationData[line];
                LocationId location = Enum.Parse<LocationId>(locationIDText);

                locationEntry.Id = location;
                locationEntry.Name = locationData[line + 1];
                locationEntry.Description = locationData[line + 2];

                LocationsData.Add(location, locationEntry);

                line += 3;
            }*/

            // Displaying title art
            foreach (string line in titleAsciiArt)
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
