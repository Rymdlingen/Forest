using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Forest
{
    #region Data types
    enum LocationId
    {
        Nowhere,
        Inventory,
        DirtyDen,
        CleanDen,
        CozyDen,
        NorthForest,
        MossyForestEntrance,
        MossyForestNorth,
        MossyForestSouth,
        BearsToilet,
        SparseForest,
        Glade,
        Forest,
        LeafyForestEntrance,
        LeafyForestNorth,
        LeafyForestMiddle,
        LeafyForestSouth,
        SouthForest,
        SouthEastForest,
        WestRiver,
        EastRiver,
        Waterfall,
        ViewPoint,
        Cliffs
    }

    // If adding here, also add in ThingIdsByName dictionary.
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
        Frog,
        Dirt
    }

    enum Direction
    {
        North,
        Northwest,
        West,
        Southwest,
        South,
        Southeast,
        East,
        Northeast,
        NoDirection
    }

    // If adding here, also add in GoalCompleted dictionary.
    enum Goal
    {
        DenCleaned,
        DenMadeCozy,
        FishEaten,
        StungByBee,
        NecklaceWorn,
        DreamtAboutShiftingShape,
        GoOnAdventure
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
    #endregion

    class Program
    {
        #region Fields
        const ConsoleColor NarrativeColor = ConsoleColor.Gray;
        const ConsoleColor PromptColor = ConsoleColor.White;
        const int PrintPauseMilliseconds = 150;

        // static List<string> load;

        // Data dictionaries.
        static Dictionary<LocationId, LocationData> LocationsData = new Dictionary<LocationId, LocationData>();
        static Dictionary<ThingId, ThingData> ThingsData = new Dictionary<ThingId, ThingData>();

        // Current state.
        static LocationId CurrentLocationId = LocationId.DirtyDen;
        static Dictionary<ThingId, LocationId> ThingsCurrentLocations = new Dictionary<ThingId, LocationId>();
        static Dictionary<Goal, bool> GoalCompleted = new Dictionary<Goal, bool> { { Goal.DenCleaned, false },
                                                                                   { Goal.DenMadeCozy, false },
                                                                                   { Goal.DreamtAboutShiftingShape, false },
                                                                                   { Goal.FishEaten, false },
                                                                                   { Goal.GoOnAdventure, false },
                                                                                   { Goal.NecklaceWorn, false },
                                                                                   { Goal.StungByBee, false } };

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
                                                                                                { "frog", ThingId.Frog },
                                                                                                { "den", ThingId.Dirt } };

        static ThingId[] ThingsYouCanGet = { ThingId.Moss, ThingId.Leaves, ThingId.Grass };
        static List<ThingId> ThingsYouCanDrop;
        static ThingId[] ThingsThatAreNpcs = { ThingId.Owl, ThingId.Frog };
        #endregion

        #region Output helpers
        /// <summary>
        /// Checks how wide the console is and writes text one line at a time making sure no words are cut off.
        /// </summary>
        /// <param name="text"></param>
        static void Print(string text)
        {
            // Split text into lines that don't exceed the window width.
            int maximumLineLength = Console.WindowWidth - 1;
            MatchCollection lineMatches = Regex.Matches(text, @"(.{1," + maximumLineLength + @"})(?:\s|$)");

            //Output each line with a small delay.
            foreach (Match line in lineMatches)
            {
                // If there is \n in the line, add an empty line at that place.
                if (line.Groups[0].Value.Contains(@"\n"))
                {
                    Console.WriteLine(line.Groups[0].Value.Split(@"\n")[0]);
                    Thread.Sleep(PrintPauseMilliseconds);
                    Console.WriteLine();
                    Thread.Sleep(PrintPauseMilliseconds);
                    Console.WriteLine(line.Groups[0].Value.Split(@"\n")[1]);
                    Thread.Sleep(PrintPauseMilliseconds);
                }
                else
                {
                    Console.WriteLine(line.Groups[0].Value);
                    Thread.Sleep(PrintPauseMilliseconds);
                }
            }
        }

        /// <summary>
        /// Checks how wide the console is and writes text one line at a time making sure no words are cut off.
        /// Adds an empty line after the text.
        /// </summary>
        /// <param name="text"></param>
        static void Reply(string text)
        {
            Print(text);
            Console.WriteLine();
        }

        /// <summary>
        /// Capitalizes the first letter in a string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>String with the first letter capitalized.</returns>
        static string Capitalize(string text)
        {
            string capitalizeFirstLetter = text[0].ToString().ToUpper();
            text = text.Remove(0, 1);
            return capitalizeFirstLetter + text;
        }
        #endregion

        #region Interaction helpers
        /// <summary>
        /// Checks every word to see if they match any Direction enum.
        /// </summary>
        /// <param name="words"></param>
        /// <returns>A list of thing ids that matched the words.</returns>
        static Direction GetDirectionFromWords(string[] words)
        {
            string[] directions = Enum.GetNames(typeof(Direction));
            Direction direction = Direction.NoDirection;

            // For every word in the entered command, check if the word is a thing.
            foreach (string word in words)
            {
                if (directions.Contains(Capitalize(word)))
                {
                    // If a word is a thing add it to the list of thing IDs.
                    direction = Enum.Parse<Direction>(Capitalize(word));
                }
            }

            return direction;
        }

        /// <summary>
        /// Checks every word to see if they match any of the keywords in the dictinarys containing all Thing Ids.
        /// </summary>
        /// <param name="words"></param>
        /// <returns>A list of thing ids that matched the words.</returns>
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

        /// <summary>
        /// Finds the words from an entered command that matches thing ids.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="thingIdsFromCommand"></param>
        /// <returns>A list of words that the player used in the command that matches thing ids.</returns>
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

        /// <summary>
        /// Checks what things have the players current location as their current location.
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns>A list of location id enumerables for all things at the players currentlocation.</returns>
        static IEnumerable<ThingId> GetThingsAtLocation(LocationId locationId)
        {
            // Returns all the ThingIds for things at the given location.
            return ThingsCurrentLocations.Keys.Where(thingId => ThingsCurrentLocations[thingId] == locationId);
        }

        /// <summary>
        /// Accesses the things data and returns the things name.
        /// </summary>
        /// <param name="thingId"></param>
        /// <returns>The things name.</returns>
        static string GetThingName(ThingId thingId)
        {
            // Returns the name of a thing.
            return ThingsData[thingId].Name;
        }

        /// <summary>
        /// Accesses the locations data and returns the locations name.
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns>The locations name.</returns>
        static string GetLocationName(LocationId locationId)
        {
            return LocationsData[locationId].Name;
        }
        #endregion

        #region Interaction
        static void HandlePlayerAction()
        {
            // Ask player what they want to do.
            Console.ForegroundColor = NarrativeColor;
            Print("Whats next?");

            Console.ForegroundColor = PromptColor;
            Console.Write("> ");

            string command = Console.ReadLine().ToLowerInvariant();

            // Split the command into words.
            char[] splitChars = { ' ', ',', '.' };
            string[] words = command.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

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
                case "northwest":
                case "nw":
                    HandleMovement(Direction.Northwest);
                    break;
                case "west":
                case "w":
                    HandleMovement(Direction.West);
                    break;
                case "southwest":
                case "sw":
                    HandleMovement(Direction.Southwest);
                    break;
                case "south":
                case "s":
                    HandleMovement(Direction.South);
                    break;
                case "southeast":
                case "se":
                    HandleMovement(Direction.Southeast);
                    break;
                case "east":
                case "e":
                    HandleMovement(Direction.East);
                    break;
                case "northeast":
                case "ne":
                    HandleMovement(Direction.Northeast);
                    break;

                // Verbs.
                case "take":
                case "pick":
                case "get":
                    HandleGet(words);
                    break;

                case "drop":
                    HandleDrop(words);
                    break;

                case "look":
                    HandleLook(words);
                    break;

                case "talk":
                    HandleTalk(words);
                    break;

                case "clean":
                    HandleClean(words);
                    break;

                case "go":
                    // TODO go to "name of location" (or direction)
                    HandleGo(words);
                    break;
                case "give":
                    // TODO needed later for giving things to NPCs
                    break;
                case "combine":
                    // TODO do I need this? probably yes, for making a fishing rod
                    break;
                case "sleep":
                    // TODO needed for the end of chapter 1 and the for the rest of the game
                    break;
                case "read":
                    // TODO not needed yet
                    break;

                // TODO interacting verbs, probably need more of them
                case "eat":
                    // TODO for eating fish
                    break;

                // Inventory.
                case "inventory":
                case "i":
                    // TODO show list if whats in the inventory
                    HandleInventory();
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
                    // TODO change text?
                    Reply("I don't understand");
                    break;
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
                DisplayNewLocation();
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
                    if (CanGetThing(thingId) && ThingIsHere(thingId))
                    {
                        Reply($"You picked up {thing}.");
                        GetThing(thingId);
                    }
                    // Thing is already in players inventory and can't be picked up again.
                    else if (HaveThing(thingId))
                    {
                        Reply($"You already have {thing} in your inventory.");
                    }
                    // Thing is in this location but can't be picked up.
                    else if (ThingIsHere(thingId) && !CanGetThing(thingId))
                    {
                        Reply($"You can't pick {thing} up.");
                    }
                    // Thing is not in this location.
                    else if (!ThingIsHere(thingId))
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
            // TODO fix droping things for making den cozy? Should I instead USE things to make den cozy? Should I give more information after a thing is droped? Specific for every thing?

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

                    // Thing is not in players inventory and can't be dropped.
                    if (!HaveThing(thingId))
                    {
                        Reply($"You don't have {thing} in your inventory.");
                    }
                    // Trying to drop things in den before den is cleaned.
                    else if (CurrentLocationId == LocationId.DirtyDen && GoalCompleted[Goal.DenCleaned] == false && (thingId == ThingId.Grass || thingId == ThingId.Leaves || thingId == ThingId.Moss))
                    {
                        // TODO fix text.
                        Reply("You should clean your den before making it cozy.");
                    }
                    // Thing is in players inventory and is dropped.
                    else if (HaveThing(thingId))
                    {
                        Reply($"You dropped {thing}.");
                        DropThing(thingId);
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // TODO text
                Reply($"There is no such thing in your inventory.");
            }
        }

        static void HandleInventory()
        {
            string[] thingIds = Enum.GetNames(typeof(ThingId));
            var thingsInInventory = new List<string>();

            // Go through all the things and find the ones that have inventory as location.
            foreach (string thing in thingIds)
            {
                ThingId thingId = Enum.Parse<ThingId>(thing);
                if (HaveThing(thingId))
                {
                    // Add all things that has inventory as location to a list.
                    thingsInInventory.Add(ThingsData[thingId].Name.ToLower());
                }
            }

            // If there is more then one thing in the players inventory, format all the things into one string.
            if (thingsInInventory.Count > 1)
            {
                // Add a new string that combines the last two things and adds "and" inbetween them.
                thingsInInventory.Add($"{thingsInInventory[thingsInInventory.Count - 2]} and {thingsInInventory[thingsInInventory.Count - 1]}");
                // Remove the two seperated words.
                thingsInInventory.RemoveRange(thingsInInventory.Count - 3, 2);

                // If there is more things then 2, add "," between all of them but the last two.
                if (thingsInInventory.Count > 1)
                {
                    // Join all words together in a string.
                    string joinList = String.Join(", ", thingsInInventory);
                    // Remove all things in the list.
                    thingsInInventory.Clear();
                    // Add the formatted string with all the things to the list.
                    thingsInInventory.Add(joinList);
                }
            }

            // If there is things in the inventory, display the list of things.
            if (thingsInInventory.Count > 0)
            {
                // TODO text
                Reply($"You have these things in your inventory: {thingsInInventory[0]}.");
            }
            // If there is no things in the inventory, tell the player that.
            else
            {
                // TODO text
                Reply("You have nothing in your inventory.");
            }
        }

        static void HandleLook(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Output the correct response depending on if the location of the thing matches the location of the player or has inventory as location.
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Thing is at players location or in inventory.
                    if (ThingIsAvailable(thingId))
                    {
                        Reply(ThingsData[thingId].Description);
                    }
                    // Thing is not in this location and not in inventory.
                    else
                    {
                        // TODO text
                        Reply($"You do not see {thing} here.");
                    }
                }
            }
            // If the player only writes look, display information about the location.
            else if (words.Count() == 1)
            {
                LookAtLocation();
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // TODO text
                Reply($"There is no such thing to look at.");
            }
        }

        static void HandleTalk(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Output the correct response depending on if the location of the thing matches the location of the player and the thing is a NPC.
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Thing is at players location and is a NPC.
                    if (ThingIsHere(thingId) && ThingIsNpc(thingId))
                    {
                        switch (thingId)
                        {
                            case ThingId.Owl:
                                // TODO
                                break;

                            case ThingId.Frog:
                                // TODO
                                break;
                        }
                    }
                    else if (!ThingIsHere(thingId) && ThingIsNpc(thingId))
                    {
                        // TODO text
                        Reply($"{Capitalize(thing)} is not here.");
                    }
                    // Thing is not something you can talk to.
                    else
                    {
                        // TODO text
                        Reply($"You can't talk to {thing}.");
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // TODO text
                Reply($"You can't talk to that.");
            }

        }

        static void HandleClean(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Output the correct response depending on if the player tries to clean the den or not.
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // The entered thing matches the dirty den.
                    if (thingId == ThingId.Dirt)
                    {
                        // TODO text
                        LocationsData[LocationId.DirtyDen].Description = LocationsData[LocationId.CleanDen].Description;
                        GoalCompleted[Goal.DenCleaned] = true;
                        Reply("You clean out all the old foliage and your den is now looking pretty good, it's time to gather new material for making it cozy for next winter.");
                    }
                    // Thing is not dirty den.
                    else
                    {
                        // TODO text
                        Reply($"{Capitalize(thing)} doesn't need cleaning.");
                    }
                }
            }
            // If the player only writes clean, ask what to clean.
            else if (words.Count() == 1)
            {
                // TODO text
                Reply("What needs cleaning?");
                // TODO add asking for input.
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // TODO text
                Reply($"That doesn't need cleaning.");
            }

        }

        static void HandleGo(string[] words)
        {
            // Getting a directiond from words found in the command.
            Direction directionFromCommand = GetDirectionFromWords(words);

            // If there is a direction.
            if (directionFromCommand != Direction.NoDirection)
            {
                HandleMovement(directionFromCommand);
            }
            // If there was no direction.
            else
            {
                // TODO text
                Reply($"I don't understand.");
            }
        }

        static void TalkToOwl()
        {
            // TODO
        }

        static void TalkToFrog()
        {
            // TODO
        }

        // TODO add more game events
        #endregion

        #region Game Rules
        static void ApplyGameRules()
        {
            if (!GoalCompleted[Goal.DenCleaned])
            {
                // Change the players location to CleanDen so I can check the current location to see if this is done?
                // Right now I will just change it to true in the handler. Maybe thats good enough?

                // TODO Change ID to the new denID aswell as the description, but keep the directions and name, also change all places that use the ID
            }

            if (!GoalCompleted[Goal.DenMadeCozy])
            {
                if (ThingAt(ThingId.Grass, LocationId.DirtyDen) && ThingAt(ThingId.Leaves, LocationId.DirtyDen) && ThingAt(ThingId.Moss, LocationId.DirtyDen))
                {
                    // TODO text
                    GoalCompleted[Goal.DenMadeCozy] = true;
                    LocationsData[LocationId.DirtyDen].Description = LocationsData[LocationId.CozyDen].Description;
                    Print($"Now your den is ready for next winter sleep.");

                    // TODO delete things from pickable things. Make ThingsYouCanGet into an dictionary with bools?
                }
            }

            if (!GoalCompleted[Goal.DreamtAboutShiftingShape])
            {
                // TODO
            }

            if (!GoalCompleted[Goal.FishEaten])
            {
                // TODO
            }

            if (!GoalCompleted[Goal.GoOnAdventure])
            {
                // TODO
            }

            if (!GoalCompleted[Goal.NecklaceWorn])
            {
                // TODO
            }

            if (!GoalCompleted[Goal.StungByBee])
            {
                // TODO
            }

            if (AllGoalsCompleted() || GoalCompleted[Goal.DenMadeCozy])
            {
                EndGame();
            }
        }
        #endregion

        #region Events
        static void EndGame()
        {
            Print("THE END! (Clearing this puzzle ends the game for now, since it's the only puzzle I have started working on)");
            quitGame = true;
            // TODO change text, and probably other things as well
        }
        #endregion

        #region Display helpers
        /// <summary>
        /// Displays all information about a location, description, directions and things.
        /// </summary>
        static void LookAtLocation()
        {
            // Display current location description.
            LocationData currentLocationData = LocationsData[CurrentLocationId];
            Print(currentLocationData.Description);
            Console.WriteLine();

            // Array with strings of directions
            string[] allDirections = Enum.GetNames(typeof(Direction));

            // Going through all the directions to se if the current locations contains a location in that direction, and displaying existing directions
            for (int direction = 0; direction < allDirections.Length; direction++)
            {
                Direction currentDirection = Enum.Parse<Direction>(allDirections[direction]);
                if (currentLocationData.Directions.ContainsKey(currentDirection))
                {
                    Print($"{allDirections[direction]}: {GetLocationName(currentLocationData.Directions[currentDirection])}");
                }
            }
            Console.WriteLine();

            // Display things at the current location, if there is any.
            IEnumerable<ThingId> thingsAtCurrentLocation = GetThingsAtLocation(CurrentLocationId);

            if (thingsAtCurrentLocation.Count() > 0)
            {
                Print("You see: ");

                foreach (ThingId thingId in thingsAtCurrentLocation)
                {
                    Print($"{GetThingName(thingId)}.");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Displays all information about a location, description, directions and things. And adds an empty line after the text.
        /// </summary>
        // Used when moving to a new location.
        static void DisplayNewLocation()
        {
            // Clears the console before displaying all the information about the location.
            Console.Clear();
            LookAtLocation();
        }
        #endregion

        #region Event helpers
        /// <summary>
        /// Checks if a certain thing is in a certain location.
        /// </summary>
        /// <param name="thingId"></param>
        /// <param name="locationId"></param>
        /// <returns>True or false.</returns>
        static bool ThingAt(ThingId thingId, LocationId locationId)
        {
            return ThingsCurrentLocations[thingId] == locationId;
        }

        /// <summary>
        /// Checks if a certain thing is at the same location as the player.
        /// </summary>
        /// <param name="thingId"></param>
        /// <returns>True or false.</returns>
        static bool ThingIsHere(ThingId thingId)
        {
            return ThingAt(thingId, CurrentLocationId);
        }

        /// <summary>
        /// Checks if a certain thing is either at the players current location or in the inventory.
        /// </summary>
        /// <param name="thingId"></param>
        /// <returns>True or false.</returns>
        static bool ThingIsAvailable(ThingId thingId)
        {
            return ThingIsHere(thingId) || HaveThing(thingId);
        }

        /// <summary>
        /// Checks if a certain thing has inventory as current location.
        /// </summary>
        /// <param name="thingId"></param>
        /// <returns>True or false.</returns>
        static bool HaveThing(ThingId thingId)
        {
            return ThingAt(thingId, LocationId.Inventory);
        }

        /// <summary>
        /// Checks if a certain thing is possible to pick up.
        /// </summary>
        /// <param name="thingId"></param>
        /// <returns>True or false.</returns>
        static bool CanGetThing(ThingId thingId)
        {
            return ThingsYouCanGet.Contains(thingId);
        }

        /// <summary>
        /// Checks if a thing is an NPC.
        /// </summary>
        /// <param name="thingId"></param>
        /// <returns>True or false.</returns>
        static bool ThingIsNpc(ThingId thingId)
        {
            return ThingsThatAreNpcs.Contains(thingId);
        }

        /// <summary>
        /// Changes a certain things location to a certain location.
        /// </summary>
        /// <param name="thingId"></param>
        /// <param name="locationId"></param>
        static void MoveThing(ThingId thingId, LocationId locationId)
        {
            ThingsCurrentLocations[thingId] = locationId;
        }

        /// <summary>
        /// Swaps two things locations.
        /// </summary>
        /// <param name="thing1Id"></param>
        /// <param name="thing2Id"></param>
        static void SwapThings(ThingId thing1Id, ThingId thing2Id)
        {
            LocationId locationOfThing1 = ThingsCurrentLocations[thing1Id];
            MoveThing(thing1Id, ThingsCurrentLocations[thing2Id]);
            MoveThing(thing2Id, locationOfThing1);
        }

        /// <summary>
        /// Changes a certain things location to inventory.
        /// </summary>
        /// <param name="thingId"></param>
        static void GetThing(ThingId thingId)
        {
            MoveThing(thingId, LocationId.Inventory);
        }

        /// <summary>
        /// Changes a certain things location to the players current location.
        /// </summary>
        /// <param name="thingId"></param>
        static void DropThing(ThingId thingId)
        {
            MoveThing(thingId, CurrentLocationId);
        }

        /// <summary>
        /// Checks if all goals are completed.
        /// </summary>
        /// <returns>True or false.</returns>
        static bool AllGoalsCompleted()
        {
            return GoalCompleted.All(goal => goal.Value);
        }
        #endregion

        #region Program start
        static void ParseData(string[] fileData)
        {
            bool newParsedDataObject = true;
            var parsedDataEntry = new ParsedData();

            // Arrays used to decide if the ParsedData object is a location or thing.
            string[] locationIdsAsStrings = Enum.GetNames(typeof(LocationId));
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
                Match match = Regex.Match(fileData[line], "^[0-9]*([A-Z].*): *[0-9]*(.*)");
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
                            string directionAndDestinationPattern = @"[ \t]*([A-Z]\w*): [0-9]+(\w*)";
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
                        if (locationIdsAsStrings.Contains(parsedDataEntry.Id))
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
            Print($"{gameStory[0]}");
            Console.ReadKey();

            // TODO Display short instructions about how to play??

            // Displaying the first location.
            DisplayNewLocation();

            // Game loop.
            while (!quitGame)
            {
                // Ask player what they want to do.
                HandlePlayerAction();
                ApplyGameRules();
            }
        }
        #endregion

        #region Start and save, WIP
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
        #endregion

        // TODO remove toilet after visiting once and change current location to 3.
        // TODO puzzle about fishing
        // TODO finding necklace
        // TODO text about trying necklace and dream
        // TODO end of part 1
    }
}
