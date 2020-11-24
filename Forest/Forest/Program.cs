using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

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
        River
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
        public LocationId StartingLocationId;
    }

    class ParsedData
    {
        public string Id;
        public string Name;
        public string Description;
        public Dictionary<Direction, LocationId> Directions;
        public string StartingLocationId;
    }

    class Program
    {
        const ConsoleColor NarrativeColor = ConsoleColor.Gray;
        const ConsoleColor PromptColor = ConsoleColor.White;
        const int PrintPauseMilliseconds = 150;

        // static List<string> load;

        // Data dictionaries.
        static Dictionary<LocationId, LocationData> LocationsData = new Dictionary<LocationId, LocationData>();
        static Dictionary<ThingId, ThingData> ThingsData = new Dictionary<ThingId, ThingData>();

        // Current state.
        static LocationId CurrentLocationId = LocationId.Den;
        static Dictionary<ThingId, LocationId> ThingsCurrentLocations = new Dictionary<ThingId, LocationId>();

        // Variable used to end the game loop and quit the game.
        static bool quitGame = false;

        // Thing helpers.
        // (Just a reminder of all the things) Moss, Grass, Leaves, Berries, Beehive, Fish, Necklace, Owl, Frog.
        static Dictionary<string, ThingId> ThingIdsByName = new Dictionary<string, ThingId>() { { "moss", ThingId.Moss },
                                                                                                { "leaves", ThingId.Leaves },
                                                                                                { "leafs", ThingId.Leaves },
                                                                                                { "leaf", ThingId.Leaves },
                                                                                                { "grass", ThingId.Grass },
                                                                                                { "berries", ThingId.Berries },
                                                                                                { "berry", ThingId.Berries },
                                                                                                { "honey", ThingId.Beehive },
                                                                                                { "fish", ThingId.Fish },
                                                                                                { "necklace", ThingId.Necklace },
                                                                                                { "owl", ThingId.Owl },
                                                                                                { "frog", ThingId.Frog } };

        static ThingId[] ThingsYouCanGet = { ThingId.Moss, ThingId.Leaves, ThingId.Grass };

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
                case "get":
                    // TODO
                    HandleGet(words);

                    break;

                case "drop":
                    // TODO
                    HandleDrop(words);

                    break;


                case "give":
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
                    // SaveCurrentGameState();
                    break;
                case "load":
                    // TODO
                    // LoadSavedGameState();
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

            // Display things at the current location, if there is any.
            IEnumerable<ThingId> thingsAtCurrentLocation = GetThingsAtLocation(CurrentLocationId);

            if (thingsAtCurrentLocation.Count() > 0)
            {
                Print("You see: ");

                foreach (ThingId thingId in thingsAtCurrentLocation)
                {
                    Print($"{GetName(thingId)}.");
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

        static void HandleGet(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Output the correct response depending on if the location of the thing matches the location of the player and if it is a pickable thing.
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Thing is pickable and at players location.
                    if (ThingsYouCanGet.Contains(thingId) && ThingsCurrentLocations[thingId] == CurrentLocationId)
                    {
                        Reply($"You picked up {thing}.");
                        ThingsCurrentLocations[thingId] = LocationId.Inventory;
                    }
                    // Thing is already in players inventory and can't be picked up again.
                    else if (ThingsCurrentLocations[thingId] == LocationId.Inventory)
                    {
                        Reply($"You already have {thing} in your inventory.");
                    }
                    // Thing is in this location but can't be picked up.
                    else if (ThingsCurrentLocations[thingId] == CurrentLocationId && !ThingsYouCanGet.Contains(thingId))
                    {
                        Reply($"You can't pick {thing} up.");
                    }
                    // Thing is not in this location.
                    else if (ThingsCurrentLocations[thingId] != CurrentLocationId)
                    {
                        Reply($"There is no {thing} here.");
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                Reply($"There is no such thing here.");
            }
        }

        static void HandleDrop(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Output the correct response depending on if the location of the thing is the inventory.
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Thing is in players inventory and is dropped.
                    if (ThingsCurrentLocations[thingId] == LocationId.Inventory)
                    {
                        Reply($"You dropped {thing}.");
                        ThingsCurrentLocations[thingId] = CurrentLocationId;
                    }
                    // Thing is not in players inventory and can't be dropped.
                    else if (ThingsCurrentLocations[thingId] != LocationId.Inventory)
                    {
                        Reply($"You don't have {thing} in your inventory.");
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                Reply($"There is no such thing in your inventory.");
            }
        }

        static List<ThingId> GetThingIdsFromWords(string[] words)
        {
            var thingIds = new List<ThingId>();

            // For every word in the entered command, check if the word is a thing.
            foreach (string word in words)
            {
                if (ThingIdsByName.ContainsKey(word))
                {
                    // If a word is a thing add it to the list of thing IDs.
                    thingIds.Add(ThingIdsByName[word]);
                }
            }

            return thingIds;
        }

        static List<string> GetThingKeysFromWords(string[] words, List<ThingId> thingIdsFromCommand)
        {
            // Getting a list of things as they are written in the entered command.
            var thingKeysFromCommand = new List<string>();
            // Searching for as many words as we found keys for Thing IDs.
            foreach (ThingId thingId in thingIdsFromCommand)
            {
                // Checking which word in the command that matches the Thing ID.
                foreach (string word in words)
                {
                    // A collection of all the keys (different words) that I decided matches the Thing IDs.
                    Dictionary<string, ThingId>.KeyCollection thingIdsByNameKeys = ThingIdsByName.Keys;
                    // If the word matches a key, add it to the list.
                    if (thingIdsByNameKeys.Contains(word) && thingId == ThingIdsByName[word])
                    {
                        thingKeysFromCommand.Add(word);
                        break;
                    }
                }
            }

            return thingKeysFromCommand;
        }

        static void ParseData(string[] fileData)
        {
            bool newParsedDataObject = true;
            var parsedDataEntry = new ParsedData();

            // Arrays used to decide if the ParsedData object is a location or thing.
            string[] locationNames = Enum.GetNames(typeof(LocationId));
            string[] thingNames = Enum.GetNames(typeof(ThingId));

            // Check every line of the file and store the information into the right place.
            for (int line = 0; line < fileData.Length; line++)
            {
                // Start of a new ParsedData object.
                if (newParsedDataObject)
                {
                    parsedDataEntry = new ParsedData();
                    parsedDataEntry.Directions = new Dictionary<Direction, LocationId>();
                    newParsedDataObject = false;
                }

                // Dividing the line into property and value.
                Match match = Regex.Match(fileData[line], "^([A-Z].*): *(.*)");
                string property = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                // Checking the property to decide where to store the value.
                switch (property)
                {
                    // TODO
                    case "Notes":
                        line = fileData.Length;
                        break;
                    case "ID":
                        parsedDataEntry.Id = value;
                        break;

                    case "Name":
                        parsedDataEntry.Name = value;
                        break;

                    case "Description":
                        parsedDataEntry.Description = value;
                        break;

                    // This case is only used for locations (not used for things).
                    case "Directions":
                        do
                        {
                            // Pattern to se if the next line is a direction.
                            string directionAndDestinationPattern = @"[ \t]*([A-Z]\w*): (\w*)";
                            Match directionAndDestination = Regex.Match(fileData[line + 1], directionAndDestinationPattern);

                            // If the next line is not a direction, break out of this case.
                            if (!Regex.IsMatch(fileData[line + 1], directionAndDestinationPattern))
                            {
                                break;
                            }

                            // Parsing the direction and destination and storing them in a dictionary.
                            Direction direction = Enum.Parse<Direction>(directionAndDestination.Groups[1].Value);
                            LocationId destination = Enum.Parse<LocationId>(directionAndDestination.Groups[2].Value);
                            parsedDataEntry.Directions[direction] = destination;

                            // Increasing the line to see if the next line is also a direction.
                            line++;

                        } while (line + 1 < fileData.Length);

                        break;

                    // This case is only used for things (not used for locations).
                    case "Starting location":
                        parsedDataEntry.StartingLocationId = value;
                        break;

                    // When the line is empty, the parsed data is used to create a LocationData or ThingData object.
                    case "":
                        // Checking if the parsed data is a location.
                        if (locationNames.Contains(parsedDataEntry.Id))
                        {
                            // Creating a new LocationData object from the parsed data.
                            // Moving data from parsedDataEntry to locationEntry.
                            LocationId locationId = Enum.Parse<LocationId>(parsedDataEntry.Id);
                            var locationEntry = new LocationData
                            {
                                Id = locationId,
                                Name = parsedDataEntry.Name,
                                Description = parsedDataEntry.Description,
                                Directions = parsedDataEntry.Directions
                            };
                            LocationsData[locationId] = locationEntry;
                        }

                        // Checking if the parsed data is a thing.
                        if (thingNames.Contains(parsedDataEntry.Id))
                        {
                            // Creating a new ThingData object from the parsed data.
                            ThingId thingId = Enum.Parse<ThingId>(parsedDataEntry.Id);
                            LocationId thingStartingLocationId = Enum.Parse<LocationId>(parsedDataEntry.StartingLocationId);
                            var thingEntry = new ThingData
                            {
                                Id = thingId,
                                Name = parsedDataEntry.Name,
                                Description = parsedDataEntry.Description,
                                StartingLocationId = thingStartingLocationId
                            };
                            ThingsData[thingId] = thingEntry;
                        }

                        // Boolean used to start creating an new ParsedData object.
                        newParsedDataObject = true;
                        break;
                }
            }
        }

        static void InitializeThingsLocations()
        {
            // Store the starting location of each thing.
            foreach (KeyValuePair<ThingId, ThingData> thingEntry in ThingsData)
            {
                ThingsCurrentLocations[thingEntry.Key] = thingEntry.Value.StartingLocationId;
            }
        }

        static IEnumerable<ThingId> GetThingsAtLocation(LocationId locationId)
        {
            // Returns all the ThingIds for things at the given location.
            return ThingsCurrentLocations.Keys.Where(thingId => ThingsCurrentLocations[thingId] == locationId);
        }

        static string GetName(ThingId thingId)
        {
            // Returns the name of a thing.
            return ThingsData[thingId].Name;
        }

        /*static string[] ReadSaveFile()
        {
            var _stream = File.OpenRead("ForestSaveFile.txt");
            var _reader = new StreamReader(_stream);

            string[] saveFiles = _reader.ReadToEnd().Trim().Split('\n');

            for (var _i = 0; _i < saveFiles.Length; ++_i)
            {
                var _score = saveFiles[_i].Split(';');

                sv_scores.Add(new Score() { iv_points = Int32.Parse(_score[0]), iv_name = _score[1] });
            }   

            _reader.Close();

            return load;

        }*/

        /*static void SaveCurrentGameState()
        {
            Print("What do you want to name your save file?");
            string saveFileName = Console.ReadLine();

            List<string[]> load = new List<string[]> { ReadSaveFile() };
            var save = File.OpenWrite("ForestSaveFile.txt");

            load.Add($"\nID: {saveFileName}");

            for (int line = 0; line < load.Length; line++)
            {
                if (load[line] == "" && load[line - 1] == "")
                {
                    var writer = new StreamWriter(save);

                    writer.WriteLine($"ID: {saveFileName}");

                    writer.Close();
                }
            }
        }*/

        /*static void LoadSavedGameState()
        {
            Print("Which save file do you want to load?");

            string[] load = ReadSaveFile();

            foreach (string line in load)
            {
                Print(line);
            }

        }*/

        static void Main(string[] args)
        {
            // Initialization.

            // Reading title art information.
            string[] titleAsciiArt = File.ReadAllLines("ForestTitleArt.txt");

            // Reading all the text for the games story.
            string[] gameStory = File.ReadAllLines("ForestGameStory.txt");

            // Reading location data.
            string[] locationData = File.ReadAllLines("ForestLocations.txt");

            // Reading thing data.
            string[] thingData = File.ReadAllLines("ForestThings.txt");

            // Parsing location and thing data.
            ParseData(locationData);
            ParseData(thingData);

            // Store all things starting locations into the dictionary of things current locations.
            InitializeThingsLocations();

            // TODO Look what computer the player is using and display a square for size if mac or use Console.SetWindowSize(); if windows

            // Displaying title art.
            foreach (string line in titleAsciiArt)
            {
                Console.WriteLine(line);
            }
            Console.ReadKey();
            Console.Clear();

            // Displaying the introduction/first part of the games story.
            Console.ForegroundColor = NarrativeColor;
            Print(gameStory[0]);
            Console.ReadKey();

            // TODO Display short instructions about how to play??

            // Displaying the first location.
            DisplayLocation();

            // Game loop.
            while (!quitGame)
            {
                // Ask player what they want to do.
                HandlePlayerAction();
            }
        }
    }
}
