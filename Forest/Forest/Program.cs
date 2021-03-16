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
        Den,
        NorthForest,
        MossyForestEntrance,
        MossyForestNorth,
        MossyForestSouth,
        BearsToilet,
        SparseForest,
        Glade,
        BeeForest,
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
        OldLeaves,
        OkLeaves,
        SoftLeaves,
        PileOfLeaves,
        Berries,
        Beehive,
        Fish,
        OldStick,
        LongStick,
        Rope,
        Trash,
        Nail,
        FishingRod,
        Necklace,
        Owl,
        Frog,
        Dirt,
        Placeholder
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
        public string[] Answers;
        public LocationId[] StartingLocationId;
    }

    class ParsedData
    {
        public string Id;
        public string Name;
        public string Description;
        public string[] Answers;
        public Dictionary<Direction, LocationId> Directions;
        public string StartingLocationId;
    }
    #endregion

    class Program
    {
        #region Fields
        const ConsoleColor NarrativeColor = ConsoleColor.DarkGreen;
        const ConsoleColor PromptColor = ConsoleColor.DarkGray;
        const int PrintPauseMilliseconds = 150;

        static bool beenTBearToiletBefore = false;

        // static List<string> load;

        // Text documents (that have data that is not parsed and stored somewhere else, or that needs to be accessed from a method without sending it as a parameter to that method).
        static string[] gameStory;
        static string[] eventAndGoalExtraText;
        static string[] defaultAnswersToGetInteractions;

        // Data dictionaries.
        static Dictionary<LocationId, LocationData> LocationsData = new Dictionary<LocationId, LocationData>();
        static Dictionary<ThingId, ThingData> ThingsData = new Dictionary<ThingId, ThingData>();

        // Current state.
        static LocationId CurrentLocationId = LocationId.Den;
        static LocationId PreviousLocation = CurrentLocationId;
        static Dictionary<ThingId, List<LocationId>> ThingsCurrentLocations = new Dictionary<ThingId, List<LocationId>>();
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
        // (Just a reminder of all the things) Moss, Grass, OldLeaves, OkLeaves, SoftLeaves, PileOfLeaves Berries, Beehive, Fish, OldStick, LongStick, Rope, Nail, FishingRod, Necklace, Owl, Frog.    
        static Dictionary<string, ThingId> ThingIdsByName = new Dictionary<string, ThingId>() { { "moss", ThingId.Moss },
                                                                                                { "leaves", ThingId.PileOfLeaves },
                                                                                                { "leafs", ThingId.PileOfLeaves },
                                                                                                { "leaf", ThingId.PileOfLeaves },
                                                                                                { "pile of leaves", ThingId.PileOfLeaves },
                                                                                                { "pile of leafs", ThingId.PileOfLeaves },
                                                                                                { "pile of leaf", ThingId.PileOfLeaves },
                                                                                                { "big leaves", ThingId.OkLeaves },
                                                                                                { "big leafs", ThingId.OkLeaves },
                                                                                                { "big leaf", ThingId.OkLeaves },
                                                                                                { "old leaves", ThingId.OldLeaves },
                                                                                                { "old leafs", ThingId.OldLeaves },
                                                                                                { "old leaf", ThingId.OldLeaves },
                                                                                                { "soft leaves", ThingId.SoftLeaves },
                                                                                                { "soft leafs", ThingId.SoftLeaves },
                                                                                                { "soft leaf", ThingId.SoftLeaves },
                                                                                                { "grass", ThingId.Grass },
                                                                                                { "berries", ThingId.Berries },
                                                                                                { "berry", ThingId.Berries },
                                                                                                { "honey", ThingId.Beehive },
                                                                                                { "fish", ThingId.Fish },
                                                                                                { "stick", ThingId.OldStick }, /*changes witch one it refers to based on location*/
                                                                                                { "old stick", ThingId.OldStick },
                                                                                                { "long stick", ThingId.LongStick },
                                                                                                { "rope", ThingId.Rope },
                                                                                                { "trash", ThingId.Trash },
                                                                                                { "garbage", ThingId.Trash },
                                                                                                { "nail", ThingId.Nail },
                                                                                                { "fishing rod", ThingId.FishingRod },
                                                                                                { "rod", ThingId.FishingRod },
                                                                                                { "necklace", ThingId.Necklace },
                                                                                                { "owl", ThingId.Owl },
                                                                                                { "frog", ThingId.Frog },
                                                                                                { "den", ThingId.Dirt } };

        static List<ThingId> ThingsYouCanGet = new List<ThingId> { ThingId.Moss, ThingId.OldLeaves, ThingId.OkLeaves, ThingId.SoftLeaves, ThingId.Grass, ThingId.LongStick, ThingId.OldStick };
        static Dictionary<ThingId, LocationId> ThingsYouCanDropAtLocations = new Dictionary<ThingId, LocationId>() { {ThingId.Moss, LocationId.Den },
                                                                                                                     {ThingId.PileOfLeaves, LocationId.Den },
                                                                                                                     {ThingId.Grass, LocationId.Den }};
        static ThingId[] ThingsThatAreNpcs = { ThingId.Owl, ThingId.Frog };
        // For puzzle: make den cozy.
        static List<ThingId> ThingsInPileOfLeaves = new List<ThingId>();
        static List<ThingId> ThingsInDen = new List<ThingId>();
        static List<ThingId> PreviousThingsInDen = new List<ThingId>();
        // For puzzle: fishing.
        static List<string> ThingsToSearch = new List<string> { "bench", "table", "blanket", "trash", "can", "trashcan", "picnic" };
        static List<ThingId> PileOfTrash = new List<ThingId> { };
        static int PiecesOfTrashCollected = 0;


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
            // If there is \n written in the text, that line is ended even if it's shorter then the maximum line length.
            MatchCollection lineMatches = Regex.Matches(text, @"([^\\]{1," + maximumLineLength + @"})(?:$|\s|\\n)");

            //Output each line with a small delay.
            foreach (Match line in lineMatches)
            {
                // If there is \n in the line, add an empty line at that place.
                if (line.Groups[0].Value.Contains(@"\n"))
                {
                    Console.WriteLine(line.Groups[0].Value.Split(@"\n")[0]);
                    Thread.Sleep(PrintPauseMilliseconds);
                    Console.WriteLine();
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

            // For every word in the entered command, check if the word is a direction.
            foreach (string word in words)
            {
                if (directions.Contains(Capitalize(word)))
                {
                    // If a word is a direction add it to the list of directions.
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
            return ThingsCurrentLocations.Keys.Where(thingId => ThingsCurrentLocations[thingId].Contains(locationId));
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

        /// <summary>
        /// Clears the consol, displayes indicator for not accepting commands(... instead of >), needs a keypress and then disapers.
        /// </summary>
        static void PressAnyKeyToContinueAndClear()
        {
            PressAnyKeyToContinue();
            Console.Clear();
        }

        /// <summary>
        /// Clears the consol, displayes indicator for not accepting commands(... instead of >), needs a keypress and then disapers.
        /// </summary>
        static void PressAnyKeyToContinue()
        {
            // TODO not sure about color
            Console.ForegroundColor = PromptColor;
            Console.Write("...");
            Console.ReadKey();
        }
        #endregion

        #region Interaction
        static void HandlePlayerAction()
        {
            // Ask player what they want to do.
            string[] words = AskForInput(eventAndGoalExtraText[38]);

            // Assuming the first word in the command is a verb. If there is no entered words the verb string stays empty.
            string verb = "";
            if (words[0] != "")
            {
                verb = words[0].Trim();
            }

            // TODO add something for if the player writes "go", "walk" or something like that, change the verb to the second word? witch should be a direction?
            // TODO add list of combined commands, and add it to the ask for input method.

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
                    // TODO for eating fish and other things.
                    break;

                // For fishing puzzle.
                case "bend":
                    // TODO for bending nail.
                    break;

                case "search":
                    HandleSearch(words);
                    break;

                case "use":
                    // TODO for using fishing rod.
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
                    // TODO ask if the player really wants to quit and if they want to save "do you really want to quit all your progress will be lost" 
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
            if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Directions[direction] == LocationId.BearsToilet)
            {
                // TODO make method or delete this.
                if (!beenTBearToiletBefore)
                {
                    LocationId oldLocation = CurrentLocationId;
                    // Changing the current location to the new location and displaying the new location information.
                    LocationId newLocation = currentLocation.Directions[direction];
                    CurrentLocationId = newLocation;
                    DisplayNewLocation();
                    PressAnyKeyToContinueAndClear();
                    CurrentLocationId = oldLocation;
                    DisplayNewLocation();
                    beenTBearToiletBefore = true;
                }
                else
                {
                    Reply("You don't want to go there again!");
                }

            }
            // If the player is going from the leafy forest to the den and have the pile of leaves, start event about leaves blowing in the wind.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.LeafyForestMiddle && currentLocation.Directions[direction] == LocationId.LeafyForestEntrance && HaveThing(ThingId.PileOfLeaves))
            {
                BringLeavesToDen();
            }
            // If the player is trying to go from the south leafy forest to the west river, they go on the waterslide and a special text is displayed as they are taken to the east part of the river because of currents in the west river.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.LeafyForestSouth && currentLocation.Directions[direction] == LocationId.WestRiver)
            {
                // Event for going down waterslide.
                GoDownWaterSlide();
            }
            // Can't go up water stream.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.WestRiver && currentLocation.Directions[direction] == LocationId.LeafyForestSouth)
            {
                // If the player tries to go from the river to the leafy forest (climbing up the waterstream).
                Print(eventAndGoalExtraText[1]);
            }
            // Normal move.
            else if (currentLocation.Directions.ContainsKey(direction))
            {
                LocationId newLocation = currentLocation.Directions[direction];
                // Changing the current location to the new location and displaying the new location information.
                MovePlayerToNewLocation(newLocation);
            }
            // No location in that direction.
            else
            {
                // Says "There is nothing of interest in that direction." (if not changed)
                Reply(eventAndGoalExtraText[28]);
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

                    // Thing is not in this location.
                    if (!ThingIsHere(thingId))
                    {
                        // Not here.
                        Reply(ThingsData[thingId].Answers[2]);

                        return;
                    }

                    // Thing is already in players inventory and can't be picked up again.
                    if (HaveThing(thingId))
                    {
                        // Already have.
                        Reply(ThingsData[thingId].Answers[3]);

                        return;
                    }

                    // Thing is in this location but can't be picked up.
                    if (ThingIsHere(thingId) && !CanGetThing(thingId))
                    {
                        // Can't pick up.
                        Reply(ThingsData[thingId].Answers[1]);

                        return;
                    }

                    // Thing is pickable and at players location.
                    switch (thingId)
                    {
                        case ThingId.OkLeaves:
                        case ThingId.OldLeaves:
                        case ThingId.SoftLeaves:
                            PickUpLeaves(thingId);
                            return;

                        default:
                            // Picked it up!
                            Reply(ThingsData[thingId].Answers[0]);
                            GetThing(thingId);
                            return;
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // Says "There is no such thing that you can pick up here." (if not changed).
                Reply(eventAndGoalExtraText[5]);
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

                    // Special case for dropping leaves in the leafy forest.
                    if (thingId == ThingId.OldLeaves || thingId == ThingId.OkLeaves || thingId == ThingId.SoftLeaves)
                    {
                        DropPileOfLeavesOutsideOfDen();

                        return;
                    }

                    // Thing is not in players inventory and can't be dropped.
                    if (!HaveThing(thingId))
                    {
                        // You don't have that in the inventory.
                        Reply(ThingsData[thingId].Answers[7]);

                        return;
                    }

                    // Cases
                    switch (thingId)
                    {
                        case ThingId.Moss:
                        case ThingId.Grass:
                        case ThingId.PileOfLeaves:
                            // Checks if the current location is the den, handles the drop accordingly.
                            DropThingInDen(thingId);
                            break;

                        // The only time leaf will refer to specific leaves are when in leafy forest, droping them makes player lose the whole pile.
                        case ThingId.OldLeaves:
                        case ThingId.OkLeaves:
                        case ThingId.SoftLeaves:
                            // Drops the whole pile of leaves.
                            DropPileOfLeavesOutsideOfDen();
                            break;

                        case ThingId.Trash:
                            DropTrash(words);
                            break;

                        // Player is trying to drop something that they can't drop here.
                        default:
                            Reply(ThingsData[thingId].Answers[5]);
                            break;
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // Says "Can't do that" (if not changed).
                Reply(eventAndGoalExtraText[26]);
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
                thingsInInventory.Add(thingsInInventory[thingsInInventory.Count - 2] + eventAndGoalExtraText[50] + thingsInInventory[thingsInInventory.Count - 1]);
                // Remove the two seperated words.
                thingsInInventory.RemoveRange(thingsInInventory.Count - 3, 2);

                // If there is more things then 2, add "," between all of them but the last two which are already combined with "and".
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
                // Says "You are carrying these things:" (if not changed).
                Reply(eventAndGoalExtraText[49] + thingsInInventory[0] + ".");
            }
            // If there is no things in the inventory, tell the player that.
            else
            {
                // Says "You are not carrying anything." (if not changed)
                Reply(eventAndGoalExtraText[48]);
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
                // Special case for pile of leaves.
                if (thingIdsFromCommand.Contains(ThingId.PileOfLeaves))
                {
                    ThingId thingId = ThingId.PileOfLeaves;

                    // Thing is at players location or in inventory.
                    if (ThingIsAvailable(thingId))
                    {
                        // Make an string array with wll the things in the pile.
                        string[] leavesInPile = new string[ThingsInPileOfLeaves.Count];
                        int count = 0;
                        foreach (ThingId leaf in ThingsInPileOfLeaves)
                        {
                            leavesInPile[count] = GetThingName(leaf).ToLower();
                            count++;
                        }

                        // Display the correct description based on what is in the pile.
                        if (ThingsInPileOfLeaves.Count == 1)
                        {
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[10], leavesInPile);
                        }
                        else if (ThingsInPileOfLeaves.Count == 2)
                        {
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[11], leavesInPile);
                        }
                        else if (ThingsInPileOfLeaves.Count == 3)
                        {
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[12], leavesInPile);
                        }
                    }
                    // Pile of leaves is not in inventory.
                    else
                    {
                        // Says "You don't have a pile of leaves to look at." (if not changed).
                        Reply(eventAndGoalExtraText[13]);
                    }
                }
                // Special case for looking at the den.
                else if (thingIdsFromCommand.Contains(ThingId.Dirt))
                {
                    // Changes depending on the state of the den.
                    LookAtDen();
                }
                else
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
                            // Says "You do not see {thing} here." (id not changed)
                            string[] thingArray = new string[] { thing };
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[47], thingArray);
                        }
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
                // Says "There is no such thing to look at." (if not changed).
                Reply(eventAndGoalExtraText[46]);
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
                        GoalCompleted[Goal.DenCleaned] = true;
                        // Changes the dens description to match it beeing clean, says "Your den is clean but it needs something to make it cozy." (if not changed)
                        ThingsData[ThingId.Dirt].Description = eventAndGoalExtraText[22];
                        // Says "You clean out all the old foliage and your den is now looking pretty good, it's time to gather new material for making it cozy for next winter." (if not changed)
                        Reply(eventAndGoalExtraText[23]);
                    }
                    // Thing is not dirty den.
                    else
                    {
                        // Says "doesn't need cleaning."
                        Reply(Capitalize(thing) + eventAndGoalExtraText[24]);
                    }
                }
            }
            // If the player only writes clean, ask what to clean.
            else if (words.Count() == 1)
            {
                // Asking "What needs cleaning?" and handles the command.
                string[] newCommand = AskForInput(eventAndGoalExtraText[45]);
                HandleClean(newCommand);
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // Says "That doesn't need cleaning." (if not changed)
                Reply(eventAndGoalExtraText[44]);
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

        static void HandleSearch(string[] words)
        {
            // Searching through things at the view point.
            if (CurrentLocationId == LocationId.ViewPoint)
            {
                SearchAtViewPoint(words);
            }
            // If the player trys to search somewhere else.
            else
            {
                Reply(eventAndGoalExtraText[43]);
            }
        }

        // TODO FLYTTA SEN   vvvv
        static void DropTrash(string[] words)
        {
            if (CurrentLocationId == LocationId.ViewPoint || CurrentLocationId == LocationId.Waterfall)
            {
                // Drop all the trash in the trash can
                PileOfTrash.Clear();
                LoseThing(ThingId.Trash);
                // Drop and lose message about putting trash in trash can.
                Reply(ThingsData[ThingId.Trash].Answers[6]);
            }
            else
            {
                if (PileOfTrash.Count > 1)
                {
                    // Drop one pice of trash, add it to the current location and display a bad message about droping trash on the ground.
                    AddThingToLocation(ThingId.Trash);
                    PileOfTrash.Remove(ThingId.Trash);
                    Reply(ThingsData[ThingId.Trash].Answers[4]);
                }
                else
                {
                    // Drop the last trash and take it away from inventory
                    DropThing(ThingId.Trash);
                    PileOfTrash.Remove(ThingId.Trash);
                    Reply(ThingsData[ThingId.Trash].Answers[4] + " " + eventAndGoalExtraText[42]);
                }
            }
        }

        /// <summary>
        /// Displayes the text from the narrative parameter and then prompts the player to write soemthing.
        /// </summary>
        /// <param name="narrative"></param>
        /// <returns>An string array with all words that the pleyer entered.</returns>
        static string[] AskForInput(string narrative)
        {
            // Display the narrative.
            Console.ForegroundColor = NarrativeColor;
            Print(narrative);

            // Line where player writes.
            Console.ForegroundColor = PromptColor;
            Console.Write("> ");

            // Asks for a command and splits it into seperate words, but also checks for matching double words and combines them.
            string[] words = SplitCommand(Console.ReadLine().ToLowerInvariant());

            return words;
        }

        /// <summary>
        /// Splits a string at everything that isn't a letter and also checks for words that become one of my key words when combined.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>An array of single words and/or combined words.</returns>
        static string[] SplitCommand(string command)
        {
            // Split the string for everything that isnt a letter.
            string[] words = Regex.Split(command, @"[^a-zA-Z]+");

            for (int word = 0; word < words.Length - 1; word++)
            {
                // Combining this word and the next.
                string twoWords = words[word] + " " + words[word + 1];
                string threeWords = "";

                if (word + 2 < words.Length)
                {
                    threeWords = twoWords + " " + words[word + 2];
                }

                // Looking for words that belong together.
                if (ThingsToSearch.Contains(twoWords) || ThingIdsByName.ContainsKey(twoWords))
                {
                    // Replace the first word with the combined word. (the second word will still be at the next spot on its own)
                    words[word] = twoWords;
                }
                else if (ThingsToSearch.Contains(threeWords) || ThingIdsByName.ContainsKey(threeWords))
                {
                    // Replace the first word with the combined word. (the second and third words will still be at the next spots on its own)
                    words[word] = threeWords;
                }
            }

            return words;
        }

        static void SearchAtViewPoint(string[] words)
        {
            // Go through all words to see if there is something searchable.
            foreach (string word in words)
            {
                // Search if there is a match.
                if (ThingsToSearch.Contains(word))
                {
                    // Gives the player a rope, trash or nothing.
                    GetTrashOrRope();

                    // If there was a search happening we do not continue with this method.
                    return;
                }
            }

            // If there was no successful search, check if the player entered something to search at all.
            if (words.Length > 1)
            {
                // The player wrote what to search but it was not a searchable thing.
                // Says "What you are trying to search through is not here." (if not changed).
                Reply(eventAndGoalExtraText[39]);
            }
            // If there was no more words than "search".
            else
            {
                // Ask for new input. Says "What do you want to search through?" (if not changed).
                string[] newWords = AskForInput(eventAndGoalExtraText[37]);
                // Search again.
                SearchAtViewPoint(newWords);
            }
        }

        static void GetTrashOrRope()
        {
            // Check how much trash the player have.
            if (PileOfTrash.Count >= 10)
            {
                // Player is carrying enough trash. Says "You didn't find anything." (if not changed).
                Reply(eventAndGoalExtraText[40]);
                return;
            }
            else if (PiecesOfTrashCollected >= 2 && !HaveThing(ThingId.Rope))
            {
                // Chance of getting rope.
                var random = new Random();
                int chanceForRope = random.Next(0, 10 - PileOfTrash.Count());

                if (chanceForRope == 0)
                {
                    // Player gets a rope.
                    GetThing(ThingId.Rope);
                    Reply(ThingsData[ThingId.Rope].Answers[0]);
                }
                else
                {
                    // Add trash to the pile.
                    PileOfTrash.Add(ThingId.Trash);
                    PiecesOfTrashCollected++;

                    // Amount of trash.
                    string[] number = new string[] { PileOfTrash.Count().ToString() };

                    // Text about how much trash the player have.
                    InsertKeyWordAndDisplay(eventAndGoalExtraText[41], number);
                }
            }
            else
            {
                // Add trash to the pile.
                PileOfTrash.Add(ThingId.Trash);
                PiecesOfTrashCollected++;

                // If the player doesn't have any trash, put the thing in the inventory.
                if (!HaveThing(ThingId.Trash))
                {
                    GetThing(ThingId.Trash);
                    Reply(ThingsData[ThingId.Trash].Answers[0]);
                }
                // If the player already had trash, display the message about picking up more trash.
                else
                {
                    // Amount of trash.
                    string[] number = new string[] { PileOfTrash.Count().ToString() };

                    // Text about how much trash the player have.
                    InsertKeyWordAndDisplay(eventAndGoalExtraText[41], number);
                }
            }
        }

        // TODO FLYTTA SEN ^^^^

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
            // Things that need to change because of a new location.
            if (PreviousLocation != CurrentLocationId)
            {
                // The key words leaf, leafs and leaves refer to the leaves at the current location.
                if (CurrentLocationId != LocationId.LeafyForestNorth && CurrentLocationId != LocationId.LeafyForestMiddle && CurrentLocationId != LocationId.LeafyForestSouth)
                {
                    // Refering to the pile of leaf that player might have in their inventory.
                    ThingIdsByName["leaves"] = ThingId.PileOfLeaves;
                    ThingIdsByName["leafs"] = ThingId.PileOfLeaves;
                    ThingIdsByName["leaf"] = ThingId.PileOfLeaves;
                }
                else
                {
                    if (CurrentLocationId == LocationId.LeafyForestMiddle)
                    {
                        // Ok leaves in the middle forest.
                        ThingIdsByName["leaves"] = ThingId.OkLeaves;
                        ThingIdsByName["leafs"] = ThingId.OkLeaves;
                        ThingIdsByName["leaf"] = ThingId.OkLeaves;
                    }
                    else if (CurrentLocationId == LocationId.LeafyForestNorth)
                    {
                        // Old leaves in the north forest.
                        ThingIdsByName["leaves"] = ThingId.OldLeaves;
                        ThingIdsByName["leafs"] = ThingId.OldLeaves;
                        ThingIdsByName["leaf"] = ThingId.OldLeaves;
                    }
                    else if (CurrentLocationId == LocationId.LeafyForestSouth)
                    {
                        // Soft leaves in the south forest.
                        ThingIdsByName["leaves"] = ThingId.SoftLeaves;
                        ThingIdsByName["leafs"] = ThingId.SoftLeaves;
                        ThingIdsByName["leaf"] = ThingId.SoftLeaves;
                    }
                }

                // Storing the current location as the previous location before moving on to the next command.
                PreviousLocation = CurrentLocationId;
            }

            // Den cleaned is changed to true in the clean command handler.
            if (!GoalCompleted[Goal.DenMadeCozy] && GoalCompleted[Goal.DenCleaned])
            {
                // First checks if there is any new things added to the den.
                if (ThingsInDen.Count > PreviousThingsInDen.Count)
                {
                    // First thing added.
                    if (ThingsInDen.Count == 1 || ThingsInDen.Count == 2)
                    {
                        LookAtDen();
                    }
                    // All things added.
                    else if (ThingsInDen.Count == 3)
                    {
                        DenGoalCompleted();
                    }

                    // Remove things already droped of from the list of things the player can pick up.
                    if (ThingsInDen.Contains(ThingId.SoftLeaves))
                    {
                        ThingsYouCanGet.Remove(ThingId.OkLeaves);
                        ThingsYouCanGet.Remove(ThingId.OldLeaves);
                        ThingsYouCanGet.Remove(ThingId.SoftLeaves);
                    }

                    if (ThingsInDen.Contains(ThingId.Moss))
                    {
                        ThingsYouCanGet.Remove(ThingId.Moss);
                    }

                    if (ThingsInDen.Contains(ThingId.Grass))
                    {
                        ThingsYouCanGet.Remove(ThingId.Grass);
                    }

                    // Set the previous den state to the current before next command.
                    PreviousThingsInDen.Clear();
                    PreviousThingsInDen.AddRange(ThingsInDen);
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

            if (AllGoalsCompleted() || (GoalCompleted[Goal.DenMadeCozy] && GoalCompleted[Goal.DenCleaned]))
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

        static void LookAtDen()
        {
            // One thing in den.
            if (ThingsInDen.Count == 1)
            {
                string[] keyWords = new string[] { GetThingName(ThingsInDen[0]).ToLower() };
                InsertKeyWordAndDisplay(eventAndGoalExtraText[15], keyWords);
            }
            // Two things in den.
            else if (ThingsInDen.Count == 2)
            {
                string[] keyWords = new string[] { GetThingName(ThingsInDen[0]).ToLower(), GetThingName(ThingsInDen[1]).ToLower() };
                InsertKeyWordAndDisplay(eventAndGoalExtraText[16], keyWords);
            }
            // All three things are in den.
            else if (ThingsInDen.Count == 3)
            {
                // Says "Your den is clean and the floor is covered in soft moss, grass and leaves, it's the coziest den you have ever made!" (if not changed)
                Reply(eventAndGoalExtraText[25]);
            }
            else
            {
                Reply(ThingsData[ThingId.Dirt].Description);
            }
        }

        static void DenGoalCompleted()
        {
            GoalCompleted[Goal.DenMadeCozy] = true;

            // Print text that tells the player the puzzle is done.
            Console.Clear();
            Reply(eventAndGoalExtraText[17]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[18]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[19]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[20]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[21]);
            PressAnyKeyToContinueAndClear();
        }

        static void DropThingInDen(ThingId thingId)
        {
            // Can only happen if player is at den.
            if (CurrentLocationId == LocationId.Den)
            {
                // Den is not clean.
                if (!GoalCompleted[Goal.DenCleaned])
                {
                    // Says "You should clean the den before putting anything in there. A good spring cleaning always makes your home feel cozier!" (if not changed).
                    Reply(eventAndGoalExtraText[6]);
                }
                // It's clean!
                else
                {
                    // Special case for dropping of leaves.
                    if (thingId == ThingId.PileOfLeaves)
                    {
                        LoseThing(thingId);
                        ThingsInDen.Add(ThingId.SoftLeaves);
                    }
                    // For moss and grass.
                    else
                    {
                        LoseThing(thingId);
                        ThingsInDen.Add(thingId);
                    }
                }
            }
            else
            {
                // If the current location is not the den, these things cant be dropped cant be droped.
                Reply(ThingsData[thingId].Answers[5]);
            }
        }

        static void DropPileOfLeavesOutsideOfDen()
        {
            if (CurrentLocationId == LocationId.LeafyForestNorth || CurrentLocationId == LocationId.LeafyForestMiddle || CurrentLocationId == LocationId.LeafyForestSouth)
            {
                if (HaveThing(ThingId.PileOfLeaves))
                {
                    Reply(ThingsData[ThingId.PileOfLeaves].Answers[6]);
                    // Remove the leaf pile from inventory and clear the list of leaves in leaf pile.
                    LoseLeafPile();
                }
                else
                {
                    // Don't have that so can't drop it.
                    Reply(eventAndGoalExtraText[36]);
                }
            }
        }

        static void PickUpLeaves(ThingId thingId)
        {
            if (ThingsInPileOfLeaves.Contains(thingId))
            {
                // Already have these leaves.
                string[] keyWords = new string[] { GetThingName(thingId).ToLower() };
                InsertKeyWordAndDisplay(eventAndGoalExtraText[7], keyWords);
            }
            else
            {
                // Get pile of leaves if player don't already have it.
                if (!ThingsCurrentLocations[ThingId.PileOfLeaves].Contains(LocationId.Inventory))
                {
                    GetThing(ThingId.PileOfLeaves);
                }

                // Put leaves in the pile.
                ThingsInPileOfLeaves.Add(thingId);
                // Display pick up line.
                Print(ThingsData[thingId].Answers[0]);

                // Display the correct information about the pick up when the pile is growing.
                if (ThingsInPileOfLeaves.Count == 2)
                {
                    // Two types of leaf in the pile.
                    string[] keyWords = new string[] { GetThingName(ThingsInPileOfLeaves[1]).ToLower(), GetThingName(ThingsInPileOfLeaves[0]).ToLower() };
                    InsertKeyWordAndDisplay(eventAndGoalExtraText[8], keyWords);
                }
                else if (ThingsInPileOfLeaves.Count == 3)
                {
                    // Three types of leaf in the pile.
                    string[] keyWords = new string[] { GetThingName(ThingsInPileOfLeaves[0]).ToLower(), GetThingName(ThingsInPileOfLeaves[1]).ToLower(), GetThingName(ThingsInPileOfLeaves[2]).ToLower() };
                    InsertKeyWordAndDisplay(eventAndGoalExtraText[9], keyWords);
                }
                else
                {
                    // If there was no extra text added, just add an empty line for formatting.
                    Console.WriteLine();
                }
            }
        }

        static void BringLeavesToDen()
        {
            Console.Clear();
            Reply(eventAndGoalExtraText[35]);

            PressAnyKeyToContinue();

            if (ThingsInPileOfLeaves.Count() == 3)
            {
                // Right order of leaves in the pile.
                if (ThingsInPileOfLeaves[0] == ThingId.SoftLeaves && ThingsInPileOfLeaves[1] == ThingId.OkLeaves && ThingsInPileOfLeaves[2] == ThingId.OldLeaves)
                {
                    // Text about the leaves the player successfully brought back.
                    Reply(eventAndGoalExtraText[14]);
                }
                // Wrong order of leaves in the pile.
                else
                {
                    // Soft leaves on top (two options, but the work with the same text).
                    if (ThingsInPileOfLeaves[2] == ThingId.SoftLeaves)
                    {
                        // Text about the important soft leaves flew away.
                        Reply(eventAndGoalExtraText[29]);
                    }
                    // There is only one wrong option when the soft leaves is in the bottom (the other option is the correct one).
                    else if (ThingsInPileOfLeaves[0] == ThingId.SoftLeaves)
                    {
                        // Text about bad mix of leaves.
                        Reply(eventAndGoalExtraText[30]);
                    }
                    else if (ThingsInPileOfLeaves[0] == ThingId.OldLeaves)
                    {
                        // Text about only having the old leaves left, bad.
                        Reply(eventAndGoalExtraText[31]);
                    }
                    else if (ThingsInPileOfLeaves[0] == ThingId.OkLeaves)
                    {
                        // Text about losing all the important soft leaves.
                        Reply(eventAndGoalExtraText[32]);
                    }

                    // Remove the leaf pile from inventory and clear the list of leaves in leaf pile.
                    LoseLeafPile();
                }
            }
            else if (ThingsInPileOfLeaves.Count() == 2)
            {
                // Not enough leaves in pile.
                Reply(eventAndGoalExtraText[33]);
                // Remove the leaf pile from inventory and clear the list of leaves in leaf pile.
                LoseLeafPile();
            }
            else
            {
                // The few leaves in the pile blew away.
                Reply(eventAndGoalExtraText[34]);
                // Remove the leaf pile from inventory and clear the list of leaves in leaf pile.
                LoseLeafPile();
            }

            PressAnyKeyToContinueAndClear();

            MovePlayerToNewLocation(LocationId.Den);
        }

        static void MovePlayerToNewLocation(LocationId newLocationId)
        {
            // Changing the current location to the new location and displaying the new location information.
            CurrentLocationId = newLocationId;
            DisplayNewLocation();
        }

        static void LoseLeafPile()
        {
            LoseThing(ThingId.PileOfLeaves);
            ThingsInPileOfLeaves.Clear();
        }

        static void LoseLeavesWhenGoingDownWaterSlide()
        {
            // Display an extra message if player was holding the pile of leaves when sliding down the river.
            if (HaveThing(ThingId.PileOfLeaves))
            {
                PressAnyKeyToContinue();
                // Text about losing the leaves.
                Print(eventAndGoalExtraText[27]);
                LoseLeafPile();
            }
        }

        static void GoDownWaterSlide()
        {
            Console.Clear();
            // Text about sliding down the river ending up in the dam.
            Reply(eventAndGoalExtraText[0]);
            // Display an extra message if player was holding the pile of leaves when sliding down the river.
            LoseLeavesWhenGoingDownWaterSlide();
            PressAnyKeyToContinueAndClear();
            // Put the player at the dam.
            MovePlayerToNewLocation(LocationId.EastRiver);
        }

        // TODO add event about floting on the river
        // TODO add event fro walking on the long path
        // TODO add event for cozy den goal completed
        // TODO add event for trying to cross the river and getting quest
        // TODO add event about bees and flowers
        // TODO add event for fishing
        // TODO event for looking through binoculars (NO, wont have time)
        #endregion

        #region Display helpers
        /// <summary>
        /// Displays all information about a location, description, directions and things.
        /// </summary>
        static void LookAtLocation()
        {
            // Display current location description.
            LocationData currentLocationData = LocationsData[CurrentLocationId];
            // Check for special text (digits)
            // Check if the corresponding bool tells the text should be shown
            // MAke a new string with all the text that should be shown
            // Print that string instead of the whole description
            Print(currentLocationData.Description);
            AddExtraDescription();
            Console.WriteLine();

            //This is now already written in the descriptions, but I will keep the code in case I change my mind.
            // Array with strings of directions.
            string[] allDirections = Enum.GetNames(typeof(Direction));

            // Going through all the directions to se if the current locations contains a location in that direction, and displaying existing directions.
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

        static void AddExtraDescription()
        {
            // Check for extra text for Den.
            if (CurrentLocationId == LocationId.Den)
            {
                // TODO color
                Console.WriteLine();

                // If den is cleaned and ready to be made cozy.
                if (GoalCompleted[Goal.DenCleaned] && !GoalCompleted[Goal.DenMadeCozy])
                {
                    Print(eventAndGoalExtraText[3]);
                }
                // If den is clean and cozy (puzzle completed).
                else if (GoalCompleted[Goal.DenMadeCozy])
                {
                    Print(eventAndGoalExtraText[4]);
                }
                // Default text, puzzle description/start.
                else
                {
                    Print(eventAndGoalExtraText[2]);
                }

                return;
            }
        }

        /// <summary>
        /// Combines keywords with an sentence and displayes it. Makes sure you are giving the sentence the right amount of words.
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="keyWords"></param>
        static void InsertKeyWordAndDisplay(string sentence, string[] keyWords)
        {
            string newSentence = "";
            int split = 0;
            foreach (string word in keyWords)
            {
                newSentence = newSentence + sentence.Split('@')[split] + word;
                split++;
            }

            newSentence = newSentence + sentence.Split('@')[split];
            Reply(newSentence);
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
            return ThingsCurrentLocations[thingId].Contains(locationId);
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
            // TODO do this really work? Will it return true if any of them are true?
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
            // TODO IS THIS WHAT I WANT???
            // Removes the thing from the current location.
            ThingsCurrentLocations[thingId].Remove(CurrentLocationId);
            // Adds the thing to a new location.
            ThingsCurrentLocations[thingId].Add(locationId);
        }

        /// <summary>
        /// Swaps two things, the one that is in the inventory is droped at players location, the other one is placed in the inventory.
        /// </summary>
        /// <param name="thing1Id"></param>
        /// <param name="thing2Id"></param>
        static void GetOneAndDropOneThing(ThingId thing1Id, ThingId thing2Id)
        {
            ThingId thingInInventory = ThingsCurrentLocations[thing1Id].Contains(LocationId.Inventory) ? thing1Id : thing2Id;
            ThingId thingNotInInventory = thingInInventory == thing1Id ? thing2Id : thing1Id;
            DropThing(thingInInventory);
            GetThing(thingNotInInventory);
        }

        /// <summary>
        /// Swaps two things, the one that is in the inventory disapears, the other one is placed in the inventory.
        /// </summary>
        /// <param name="thing1Id"></param>
        /// <param name="thing2Id"></param>
        static void GetOneAndLoseOneThing(ThingId thing1Id, ThingId thing2Id)
        {
            ThingId thingInInventory = ThingsCurrentLocations[thing1Id].Contains(LocationId.Inventory) ? thing1Id : thing2Id;
            ThingId thingNotInInventory = thingInInventory == thing1Id ? thing2Id : thing1Id;
            LoseThing(thingInInventory);
            GetThing(thingNotInInventory);
        }

        /// <summary>
        /// Duplicates and adds a certain thing to the inventory.
        /// </summary>
        /// <param name="thingId"></param>
        static void GetThing(ThingId thingId)
        {
            ThingsCurrentLocations[thingId].Add(LocationId.Inventory);
        }

        /// <summary>
        /// Changes a certain things location from inventory to the players current location.
        /// </summary>
        /// <param name="thingId"></param>
        static void DropThing(ThingId thingId)
        {
            ThingsCurrentLocations[thingId].Remove(LocationId.Inventory);
            ThingsCurrentLocations[thingId].Add(CurrentLocationId);
        }

        /// <summary>
        /// Delete a certain thing from the inventory.
        /// </summary>
        /// <param name="thingId"></param>
        static void LoseThing(ThingId thingId)
        {
            ThingsCurrentLocations[thingId].Remove(LocationId.Inventory);
        }

        /// <summary>
        /// Add a thing to the current location, without losing a thing.
        /// </summary>
        /// <param name="thingId"></param>
        static void AddThingToLocation(ThingId thingId)
        {
            ThingsCurrentLocations[thingId].Add(CurrentLocationId);
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
                    case "Answers":
                        // Create a list of all the answers (using a list so I can add one answer at the time)
                        var listOfAnswers = new List<string>();

                        // Counting the lines with answers, there is 9 lines with answers.
                        int lines = 0;
                        int numberOfAnswers = 9;

                        // Pattern for finding just the text that is the answer.
                        string answerPattern = @"[ \t]*.*: *(.*)";

                        // Go through all the 8 answers to see if there is a custom one otherwise add the default answer.
                        do
                        {
                            // Searching in the current line + the number of answer lines we are at + on because we started on the line that guides the code to start parsing.
                            string answer = Regex.Match(fileData[line + lines + 1], answerPattern).Groups[1].Value;

                            // If I forgot to delete the template text from the data, the answer should be the default one,
                            if (answer.Contains('<') || answer.Contains('>'))
                            {
                                answer = "";
                            }

                            // Store the correct type of answer.
                            if (answer == "")
                            {
                                // No custom answer, add the default answer to the list.
                                listOfAnswers.Add(defaultAnswersToGetInteractions[lines].Split(':', '@')[1].TrimStart() + parsedDataEntry.Name.ToLower() + defaultAnswersToGetInteractions[lines].Split('@')[1]);
                            }
                            else
                            {
                                // There is a custom answer, add it to the list.
                                listOfAnswers.Add(answer);
                            }
                            lines++;

                        } while (lines < numberOfAnswers);

                        line += numberOfAnswers;

                        // Make the list into an array and add it to the data.
                        parsedDataEntry.Answers = listOfAnswers.ToArray<string>();

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
                            var startingLocationsStrings = new List<string>(parsedDataEntry.StartingLocationId.Split(','));
                            var startingLocationsIds = new List<LocationId>();
                            for (int location = 0; location < startingLocationsStrings.Count; location++)
                            {
                                if (location == 0)
                                {
                                    startingLocationsIds.Add(Enum.Parse<LocationId>(startingLocationsStrings[location].Trim()));
                                }
                                else
                                {
                                    startingLocationsIds.Add(Enum.Parse<LocationId>(RemoveDigits(startingLocationsStrings[location].Trim())));
                                }
                            }
                            LocationId[] thingStartingLocationId = startingLocationsIds.ToArray<LocationId>();
                            var thingEntry = new ThingData
                            {
                                Id = thingId,
                                Name = parsedDataEntry.Name,
                                Description = parsedDataEntry.Description,
                                Answers = parsedDataEntry.Answers,
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

        /// <summary>
        /// Removes empty lines from an array.
        /// </summary>
        /// <param name="arrayWithText"></param>
        /// <returns>Array with no empty entries.</returns>
        static string[] RemoveEmptyLines(string[] arrayWithText)
        {
            // Make the array into a list.
            var listOfText = new List<string>(arrayWithText.ToList());

            // Check each line in the list to see if they are empty.
            for (int line = 0; line < listOfText.Count; line++)
            {
                // If the line is empty remove it.
                if (listOfText[line] == "")
                {
                    listOfText.RemoveAt(line);
                    // Since a line was removed, all upcomming lines got a lower number, adjust the line index accordingly.
                    line--;
                }
            }

            // Make the list into an array and return it.
            string[] noEmptyLines = listOfText.ToArray<string>();
            return noEmptyLines;
        }

        /// <summary>
        /// Removes all digits in the start of a string. (Make sure the first char is actually a digit before calling)
        /// </summary>
        /// <param name="textWithDigits"></param>
        /// <returns>A string without digits.</returns>
        static string RemoveDigits(string textWithDigits)
        {
            // Remove the first char
            string text = textWithDigits.Remove(0, 1);

            // Check if next char is a digit.
            if (Regex.IsMatch(text[0].ToString(), @"\d"))
            {
                // If it is call this method again.
                text = RemoveDigits(text);
            }

            // Return a new string.
            string noDigits = text;
            return noDigits;
        }

        /// <summary>
        /// Calls both the remove empty lines and the remove digits method.
        /// </summary>
        /// <param name="arrayWithText"></param>
        /// <returns>An array of string that are not empty and do not start with digits.</returns>
        static string[] RemoveEmptyLinesAndDigits(string[] arrayWithText)
        {
            // Call the remove empty lines method.
            string[] processedText = RemoveEmptyLines(arrayWithText);

            // Go through all the lines to see if they start with a digit.
            int lineIndex = 0;
            foreach (string line in processedText)
            {
                if (Regex.IsMatch(line[0].ToString(), @"\d"))
                {
                    // If they do call the remove digit method.
                    processedText[lineIndex] = RemoveDigits(line);
                }
                // Used for overwriting the correct line in the array.
                lineIndex++;
            }

            // Return an array with no empty lines and no digits in the start of lines.
            return processedText;
        }

        static void InitializeThingsLocations()
        {
            // Store the starting location(s) of each thing.
            foreach (KeyValuePair<ThingId, ThingData> thingEntry in ThingsData)
            {
                foreach (LocationId location in thingEntry.Value.StartingLocationId)
                {//could not find key Dirt CONTINUE HERE
                    if (ThingsCurrentLocations.ContainsKey(thingEntry.Key))
                    {
                        ThingsCurrentLocations[thingEntry.Key].Add(location);
                    }
                    else
                    {
                        ThingsCurrentLocations.Add(thingEntry.Key, new List<LocationId>());
                        ThingsCurrentLocations[thingEntry.Key].Add(location);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            // Initialization.

            // Reading title art information.
            string[] titleAsciiArt = File.ReadAllLines("ForestTitleArt.txt");

            // Reading all the text for the games story.
            gameStory = File.ReadAllLines("ForestGameStory.txt");

            // Reading all extra event and gaol text.
            eventAndGoalExtraText = RemoveEmptyLinesAndDigits(File.ReadAllLines("ForestEventAndGoalText.txt"));

            defaultAnswersToGetInteractions = RemoveEmptyLinesAndDigits(File.ReadAllLines("ForestDefaultAnswers.txt"));

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
