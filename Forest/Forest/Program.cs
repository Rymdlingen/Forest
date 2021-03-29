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
        Nuts,
        Mushrooms,
        Flower,
        Flowers,
        Bee,
        Bees,
        Honey,
        Fish,
        OldStick,
        LongStick,
        Rope,
        Trash,
        Notes,
        Binoculars,
        Nail,
        FishingRodLong,
        FishingRodOld,
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
        EnoughFoodConsumed,
        HaveRelaxed,

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
                                                                                   { Goal.EnoughFoodConsumed, false },
                                                                                   { Goal.HaveRelaxed, false },

                                                                                   { Goal.DreamtAboutShiftingShape, false },
                                                                                   { Goal.GoOnAdventure, false },
                                                                                   { Goal.NecklaceWorn, false },
                                                                                   { Goal.StungByBee, false } };

        // Variable used to end the game loop and quit the game.
        static bool quitGame = false;

        // Direction helpers.
        static Dictionary<string, Direction> DirectionIdsByName = new Dictionary<string, Direction>() { { "n", Direction.North },
                                                                                                    { "north", Direction.North },
                                                                                                    { "ne", Direction.Northeast },
                                                                                                    { "northeast", Direction.Northeast },
                                                                                                    { "north east", Direction.Northeast }, // TODO Make it work with space
                                                                                                    { "e", Direction.East },
                                                                                                    { "east", Direction.East },
                                                                                                    { "se", Direction.Southeast },
                                                                                                    { "southeast", Direction.Southeast },
                                                                                                    { "south east", Direction.Southeast },
                                                                                                    { "s", Direction.South },
                                                                                                    { "south", Direction.South },
                                                                                                    { "sw", Direction.Southwest },
                                                                                                    { "southwest", Direction.Southwest },
                                                                                                    { "south west", Direction.Southwest },
                                                                                                    { "w", Direction.West },
                                                                                                    { "west", Direction.West },
                                                                                                    { "nw", Direction.Northwest },
                                                                                                    { "northwest", Direction.Northwest },
                                                                                                    { "north west", Direction.Northwest } };


        // Thing helpers.
        // (Just a reminder of all the things) Moss, Grass, OldLeaves, OkLeaves, SoftLeaves, PileOfLeaves Berries, Beehive, Fish, OldStick, LongStick, Rope, Nail, FishingRod, Necklace, Owl, Frog.    
        static Dictionary<string, ThingId> ThingIdsByName = new Dictionary<string, ThingId>() { { "moss", ThingId.Moss },
                                                                                                { "leaves", ThingId.PileOfLeaves },
                                                                                                { "leafs", ThingId.PileOfLeaves },
                                                                                                { "leaf", ThingId.PileOfLeaves },
                                                                                                { "pile of leaves", ThingId.PileOfLeaves },
                                                                                                { "pile of leafs", ThingId.PileOfLeaves },
                                                                                                { "pile of leaf", ThingId.PileOfLeaves },
                                                                                                { "pile", ThingId.PileOfLeaves },
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
                                                                                                { "blue", ThingId.Berries },
                                                                                                { "red", ThingId.Berries },
                                                                                                { "blueberries", ThingId.Berries },
                                                                                                { "blueberry", ThingId.Berries },
                                                                                                { "redberries", ThingId.Berries },
                                                                                                { "redberry", ThingId.Berries },
                                                                                                { "nuts", ThingId.Nuts },
                                                                                                { "nut", ThingId.Nuts },
                                                                                                { "mushrooms", ThingId.Mushrooms },
                                                                                                { "mushroom", ThingId.Mushrooms },
                                                                                                { "fungi", ThingId.Mushrooms },
                                                                                                { "fungis", ThingId.Mushrooms },
                                                                                                { "fungus", ThingId.Mushrooms },
                                                                                                { "funguses", ThingId.Mushrooms },
                                                                                                { "shroom", ThingId.Mushrooms },
                                                                                                { "shrooms", ThingId.Mushrooms },
                                                                                                { "chanterelle", ThingId.Mushrooms },
                                                                                                { "chanterelles", ThingId.Mushrooms },
                                                                                                { "flower", ThingId.Flower },
                                                                                                { "flowers", ThingId.Flowers },
                                                                                                { "bee", ThingId.Bee },
                                                                                                { "bees", ThingId.Bees },
                                                                                                { "honey", ThingId.Honey },
                                                                                                { "fish", ThingId.Fish },
                                                                                                { "fishes", ThingId.Fish },
                                                                                                { "stick", ThingId.OldStick },
                                                                                                { "old stick", ThingId.OldStick },
                                                                                                { "long stick", ThingId.LongStick },
                                                                                                { "rope", ThingId.Rope },
                                                                                                { "trash", ThingId.Trash },
                                                                                                { "garbage", ThingId.Trash },
                                                                                                { "note", ThingId.Notes },
                                                                                                { "notes", ThingId.Notes },
                                                                                                { "binoculars", ThingId.Binoculars },
                                                                                                { "telescope", ThingId.Binoculars },
                                                                                                { "nail", ThingId.Nail },
                                                                                                { "fishing rod", ThingId.FishingRodOld },
                                                                                                { "rod", ThingId.FishingRodOld },
                                                                                                { "necklace", ThingId.Necklace },
                                                                                                { "dazzle", ThingId.Necklace },
                                                                                                { "glow", ThingId.Necklace },
                                                                                                { "something", ThingId.Necklace },
                                                                                                { "thing", ThingId.Necklace },
                                                                                                { "bush", ThingId.Necklace },
                                                                                                { "bushes", ThingId.Necklace },
                                                                                                { "reflection", ThingId.Necklace },
                                                                                                { "light", ThingId.Necklace },
                                                                                                { "owl", ThingId.Owl },
                                                                                                { "frog", ThingId.Frog },
                                                                                                { "den", ThingId.Dirt } };

        // TODO i think some food should be added to the list of things you can get
        static List<ThingId> ThingsYouCanGet = new List<ThingId> { ThingId.Moss, ThingId.OldLeaves, ThingId.OkLeaves, ThingId.SoftLeaves, ThingId.Grass, ThingId.LongStick, ThingId.OldStick, ThingId.Trash, ThingId.Fish, ThingId.Flower, ThingId.Honey, ThingId.Necklace };
        static Dictionary<ThingId, LocationId> ThingsYouCanDropAtLocations = new Dictionary<ThingId, LocationId>() { { ThingId.Flower, LocationId.BeeForest } };
        static ThingId[] ThingsThatAreNpcs = { ThingId.Owl, ThingId.Frog };
        // For puzzle: make den cozy.
        static List<ThingId> ThingsInPileOfLeaves = new List<ThingId>();
        static List<ThingId> ThingsInDen = new List<ThingId>();
        static List<ThingId> PreviousThingsInDen = new List<ThingId>();
        // For puzzle: fishing.
        static bool FishingPuzzleStarted = false;
        static List<string> ThingsToSearch = new List<string> { "bench", "benches", "table", "tables", "blanket", "blankets", "trash", "can", "trashcan", "picnic", "mess", "fence", "ground", "grill" };
        static List<ThingId> PileOfTrash = new List<ThingId> { };
        static int PiecesOfTrashCollected = 0;
        static List<string> CorrectArrows = new List<string> { "left", "right", "down", "left", "left", "up" };
        static List<string> EnteredArrows = new List<string>();
        static bool HiddenPathFound = false;
        static bool NailFound = false;
        // For puzzle: lost bee and honey.
        static bool BeeHasBeenInBeeForest = false;
        static bool BeeIsHome = false;
        static bool HoneyPuzzleStarted = false;
        static bool HaveTriedToEatFlower = false;
        // For puzzle: necklace.
        static bool HaveTriedToSwimOverRiver = false;
        static List<ThingId> ListOfEatenThings = new List<ThingId>();
        static bool DidSleep = false;
        #endregion

        #region Output helpers
        /// <summary>
        /// Checks how wide the console is and writes text one line at a time making sure no words are cut off.
        /// </summary>
        /// <param name="text"></param>
        static void Print(string text)
        {
            // Split text into lines that don't exceed the window width.
            int maximumLineLength = Console.WindowWidth - 2;
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
        /// Special method for choosing between old and long stick (dosn't make a difference between having a thing
        /// </summary>
        static ThingId AskWitchStick()
        {
            ThingId choosenStick = ThingId.Placeholder;

            // The player has to choose what stick to use.

            // Ask witch one they mean.
            Console.WriteLine();
            string[] words = AskForInput(eventAndGoalExtraText[76]);

            for (int word = 0; word < words.Length; word++)
            {
                if (words[word] == "old")
                {
                    words[word] = "old stick";
                }
                else if (words[word] == "long")
                {
                    words[word] = "long stick";
                }
                else if (words[word] == "stick")
                {
                    words[word] = "";
                }
            }

            var thingIds = new List<ThingId>(GetThingIdsFromWords(words));

            // Choose a stick based on the input.
            foreach (ThingId thingId in thingIds)
            {
                if (thingId == ThingId.LongStick)
                {
                    return choosenStick = ThingId.LongStick;
                }
                else if (thingId == ThingId.OldStick)
                {
                    return choosenStick = ThingId.OldStick;
                }
            }

            if (ThingIsAvailable(ThingId.LongStick) && ThingIsAvailable(ThingId.OldStick))
            {
                // Text about not understanding.
                Console.WriteLine();
                Reply(eventAndGoalExtraText[77]);
            }

            return choosenStick;
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
                if (ThingsToSearch.Contains(twoWords) || ThingIdsByName.ContainsKey(twoWords) || DirectionIdsByName.ContainsKey(twoWords))
                {
                    // Replace the first word with the combined word (the second word is cleared).
                    words[word] = twoWords;
                    words[word + 1] = "";
                }
                else if (ThingsToSearch.Contains(threeWords) || ThingIdsByName.ContainsKey(threeWords))
                {
                    // Replace the first word with the combined word (the second and third words is cleared).
                    words[word] = threeWords;
                    words[word + 1] = "";
                    words[word + 2] = "";
                }
            }

            return words;
        }

        /// <summary>
        /// Checks every word to see if they match any Direction enum.
        /// </summary>
        /// <param name="words"></param>
        /// <returns>The first found direction.</returns>
        static Direction GetDirectionFromWords(string[] words)
        {
            Direction direction = Direction.NoDirection;

            // For every word in the entered command, check if the word is a direction.
            foreach (string word in words)
            {
                // It directions by name contains the word as a key.
                if (DirectionIdsByName.ContainsKey(word))
                {
                    // If a word is a direction add it to the list of directions.
                    direction = DirectionIdsByName[word];

                    // If a direction was found break out of the loop.
                    break;
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
            Console.CursorLeft = 0;
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
                case "north west":
                case "nw":
                    HandleMovement(Direction.Northwest);
                    break;
                case "west":
                case "w":
                    HandleMovement(Direction.West);
                    break;
                case "southwest":
                case "south west":
                case "sw":
                    HandleMovement(Direction.Southwest);
                    break;
                case "south":
                case "s":
                    HandleMovement(Direction.South);
                    break;
                case "southeast":
                case "south east":
                case "se":
                    HandleMovement(Direction.Southeast);
                    break;
                case "east":
                case "e":
                    HandleMovement(Direction.East);
                    break;
                case "northeast":
                case "north east":
                case "ne":
                    HandleMovement(Direction.Northeast);
                    break;

                // Verbs.
                case "take":
                case "pick":
                case "get":
                    HandleGet(words);
                    break;

                case "place":
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

                case "swim":
                    HandleSwim(words);
                    break;

                case "give":
                    // TODO needed later for giving things to NPCs
                    break;
                case "combine":
                    // TODO do I need this? probably yes, for making a fishing rod
                    break;
                case "sleep":
                    HandleSleep();
                    break;
                case "read":
                    // TODO not needed yet
                    break;

                // TODO interacting verbs, probably need more of them
                case "eat":
                    // TODO for eating fish and other things.
                    HandleEat(words);
                    break;

                // For fishing puzzle.
                case "search":
                    HandleSearch(words);
                    break;

                case "use":
                    // TODO for using fishing rod adn binoculars.
                    HandleUse(words);
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
                    Reply("I don't understand ");
                    break;
            }
        }

        static void HandleMovement(Direction direction)
        {
            LocationData currentLocation = LocationsData[CurrentLocationId];

            // Checking if the direction is availible for the current location.
            // For trying to go south over river to cliffs.
            if (direction == Direction.South && CurrentLocationId == LocationId.WestRiver)
            {
                // Checks if player can swim over river or not, moves player if nessesary and displayes the right text.
                SwimOverRiver();
            }
            // For giving instructions about eating a flower.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.BeeForest && currentLocation.Directions[direction] == LocationId.Glade && BeeIsHome && !HoneyPuzzleStarted)
            {
                // Will trigger the start of eating flowers once.
                GetIdeaAboutHoney();
            }
            // If the player is going from the leafy forest to the den and have the pile of leaves, start event about leaves blowing in the wind.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.LeafyForestMiddle && currentLocation.Directions[direction] == LocationId.LeafyForestEntrance && HaveThing(ThingId.PileOfLeaves))
            {
                // Move the bee if the previous location had the bee and player have the flower.
                MoveBee(LocationId.Den);

                // Tries to bring the leaves to the den.
                BringLeavesToDen();
            }
            // If the player is trying to go from the south leafy forest to the west river, they go on the waterslide and a special text is displayed as they are taken to the east part of the river because of currents in the west river.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.LeafyForestSouth && currentLocation.Directions[direction] == LocationId.WestRiver)
            {
                // Move the bee if the previous location had the bee and player have the flower.
                MoveBee(LocationId.EastRiver);

                // Event for going down waterslide.
                GoDownWaterSlide();
            }
            // Can't go up water stream.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.WestRiver && currentLocation.Directions[direction] == LocationId.LeafyForestSouth)
            {
                // If the player tries to go from the river to the leafy forest (climbing up the waterstream).
                Print(eventAndGoalExtraText[1]);
            }
            // When moving from waterfall to old tree for the first time, event about finding nail starts.
            else if (currentLocation.Directions.ContainsKey(direction) && currentLocation.Id == LocationId.Waterfall && currentLocation.Directions[direction] == LocationId.SouthEastForest && !NailFound)
            {
                // Move the bee if the previous location had the bee and player have the flower.
                MoveBee(LocationId.SouthEastForest);

                // Text about finding nail.
                FindNail();
            }
            // Normal move.
            else if (currentLocation.Directions.ContainsKey(direction))
            {
                LocationId newLocation = currentLocation.Directions[direction];

                // Move the bee if the previous location had the bee and player have the flower.
                MoveBee(newLocation);

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

                    // Special case for necklace.
                    if (thingId == ThingId.Necklace && ThingIsHere(ThingId.Necklace))
                    {
                        // If the thing is the necklace and the necklace is here.
                        FindNecklace();

                        return;
                    }
                    else if (thingId == ThingId.Necklace && thingIdsFromCommand.Count == 1)
                    {
                        // If the thing is the necklace and the necklace is not here, but there was mor things found in the command, then check the next thing.
                        continue;
                    }
                    else if (thingId == ThingId.Necklace)
                    {
                        // If the thing is the necklace but the necklace is not here.
                        // Says "There is no such thing that you can pick up here." (if not changed).
                        Reply(eventAndGoalExtraText[5]);
                        return;
                    }

                    // Thing is not in this location.
                    if (!ThingIsHere(thingId))
                    {
                        // Not here.
                        Reply(ThingsData[thingId].Answers[2]);

                        return;
                    }

                    // Thing is already in players inventory and can't be picked up again (except for if the thing is trash).
                    if (HaveThing(thingId) && thingId != ThingId.Trash)
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
                        case ThingId.Grass:
                            PickUpGrassAndGetFlower();
                            return;

                        case ThingId.OkLeaves:
                        case ThingId.OldLeaves:
                        case ThingId.SoftLeaves:
                            PickUpLeaves(thingId);
                            return;

                        case ThingId.Trash:
                            PickUpTrash();
                            return;

                        case ThingId.Fish:
                            StartFishingPuzzle();
                            return;

                        case ThingId.Honey:
                            // Picked it up!
                            Reply(ThingsData[thingId].Answers[0]);
                            MoveThing(thingId, CurrentLocationId, LocationId.Inventory);
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
                        // TODO Could make it so the correct type of leafs is dropped if specified, othervise the whole pile. Also need to know if player writes pile of leaf
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
                            DropTrash();
                            break;

                        case ThingId.Flower:
                            DropFlower();
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
            var thingsInInventoryList = new List<string>();

            // Go through all the things and find the ones that have inventory as location.
            foreach (string thing in thingIds)
            {
                ThingId thingId = Enum.Parse<ThingId>(thing);
                if (HaveThing(thingId))
                {
                    // Add all things that has inventory as location to a list.
                    thingsInInventoryList.Add(ThingsData[thingId].Name.ToLower());
                }
            }

            // If there is things in the inventory, display the list of things.
            if (thingsInInventoryList.Count > 0)
            {
                string thingsInInventory = FormatListIntoString(thingsInInventoryList);

                // Says "You are carrying these things:" (if not changed).
                Reply(eventAndGoalExtraText[49] + thingsInInventory + ".");

                // If the player looks in the inventory, have the puzzle about fishing started and have all the things to make a fishing rod, the things combine to a fishing rod.
                if (FishingPuzzleStarted && HaveThing(ThingId.Rope) && HaveThing(ThingId.Nail) && (HaveThing(ThingId.OldStick) || HaveThing(ThingId.LongStick)))
                {
                    PressAnyKeyToContinue();
                    CombineToFishingRod();
                }
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
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Special case for finding necklace.
                    if (thingId == ThingId.Necklace && ThingIsHere(ThingId.Necklace))
                    {
                        // If the thing is the necklace and the necklace is here.
                        FindNecklace();

                        return;
                    }
                    // Special case for using a word that refers to the necklace when it is not around but there is more things found in the command.
                    else if (thingId == ThingId.Necklace && !HaveThing(ThingId.Necklace) && thingIdsFromCommand.Count == 1)
                    {
                        // If the thing is the necklace and the necklace is not here, but there was more things found in the command, then check the next thing instead.
                        continue;
                    }
                    // Special case for pile of leaves.
                    else if (thingId == ThingId.PileOfLeaves)
                    {
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

                        // Only lock at the first thing.
                        return;
                    }
                    // Special case for looking at the den.
                    else if (thingId == ThingId.Dirt)
                    {
                        // Changes depending on the state of the den.
                        LookAtDen();

                        // Only lock at the first thing.
                        return;
                    }
                    // Special case for looking at trash, tells you about trash you see or that there is none.
                    else if (thingId == ThingId.Trash)
                    {
                        // Different depending on how much trash player has and depending on location.
                        LookAtTrash();

                        // Only lock at the first thing.
                        return;
                    }
                    // Special case for looking at stick.
                    else if (thingId == ThingId.LongStick || thingId == ThingId.OldStick)
                    {
                        LookAtStick(words);

                        // Only lock at the first thing.
                        return;
                    }
                    // Special case for looking at fish and puzzle about fishing is not started.
                    else if (thingId == ThingId.Fish)
                    {
                        if (HaveThing(ThingId.Fish))
                        {
                            Reply(ThingsData[ThingId.Fish].Description);
                        }
                        else if (!FishingPuzzleStarted)
                        {
                            // Trigger the event for starting the fishing puzzle.
                            StartFishingPuzzle();
                        }
                        else if (FishingPuzzleStarted)
                        {
                            // Description about how player is trying to catch the fish.
                            Reply(eventAndGoalExtraText[99]);
                        }
                    }
                    else
                    {
                        // Thing is at players location or in inventory.
                        if (ThingIsAvailable(thingId))
                        {
                            Reply(ThingsData[thingId].Description);
                        }
                        // Thing is not in this location and not in inventory.
                        else
                        {
                            // Says "You do not see {thing} here." (if not changed)
                            string[] thingArray = new string[] { thing };
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[47], thingArray);
                        }

                        // Only lock at the first thing.
                        return;
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
                        Reply(Capitalize(thing) + " " + eventAndGoalExtraText[24]);
                    }
                }
            }
            // If the player only writes clean, ask what to clean.
            else if (words.Count() == 1)
            {
                Console.WriteLine();
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

        static void HandleSwim(string[] words)
        {
            foreach (string word in words)
            {
                // If the player is trying to swim south in the south leafy forest (down the bigger water stream) or the west river (to cross the river).
                if (DirectionIdsByName.ContainsKey(word))
                {
                    if (DirectionIdsByName[word] == Direction.South && (CurrentLocationId == LocationId.WestRiver || CurrentLocationId == LocationId.LeafyForestSouth))
                    {
                        HandleGo(words);
                        return;
                    }
                }
            }

            bool containsDirection = false;

            foreach (string word in words)
            {
                // If the player is trying to swim south in the south leafy forest (down the bigger water stream) or the west river (to cross the river).
                if (DirectionIdsByName.ContainsKey(word))
                {
                    containsDirection = true;
                    break;
                }
            }

            // If the player tries to swim at a location with water (and not go in a direction).
            if (!containsDirection && (CurrentLocationId == LocationId.SparseForest || CurrentLocationId == LocationId.LeafyForestNorth || CurrentLocationId == LocationId.LeafyForestMiddle || CurrentLocationId == LocationId.LeafyForestSouth))
            {
                // Taking a small bath in a stream.
                Reply(eventAndGoalExtraText[131]);
            }
            else if (!containsDirection && (CurrentLocationId == LocationId.WestRiver || CurrentLocationId == LocationId.Waterfall))
            {
                // No swim, dangerous water to swim around in.
                Reply(eventAndGoalExtraText[132]);
            }
            else if (CurrentLocationId == LocationId.EastRiver && !containsDirection)
            {
                // Takes a swim in the dam.
                Reply(eventAndGoalExtraText[133]);
            }
            else if (!containsDirection)
            {
                // No swiming here.
                Reply(eventAndGoalExtraText[134]);
            }
            // If the player tries to swim in a direction.
            else
            {
                // No swiming in that direction.
                Reply(eventAndGoalExtraText[135]);
            }
        }

        static void HandleSleep()
        {
            if (CurrentLocationId == LocationId.Den && HaveThing(ThingId.Necklace) && !GoalCompleted[Goal.DreamtAboutShiftingShape])
            {
                // Bear sleeps in den, dreams about shape shifting and waking up as animal.
                SleepAndDream();
            }
            else if (HaveThing(ThingId.Necklace))
            {
                // Says "You should sleep in the den, you worked so hard to make it cozy." (if not changed).
                Reply(eventAndGoalExtraText[154]);
            }
            else if (CurrentLocationId == LocationId.Den && HaveTriedToSwimOverRiver)
            {
                // Says "You should go wind down for a bit before going to sleep." (if not changed).
                Reply(eventAndGoalExtraText[155]);
            }
            else if (CurrentLocationId == LocationId.Den)
            {
                // Says "No more sleep before night, there's lots to get done!" (if not changed).
                Reply(eventAndGoalExtraText[156]);
            }
            else
            {
                // Says "There's no time fo sleeping, lots to get done today!" (if not changed).
                Reply(eventAndGoalExtraText[157]);
            }
        }

        // TODO MOVE LATER vvvvv

        static void SleepAndDream()
        {
            // Bear goes to bed and immediately falls alseep.
            Reply(eventAndGoalExtraText[158]);
            PressAnyKeyToContinueAndClear();

            // Dream
            Reply(eventAndGoalExtraText[159]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[160]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[161]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[162]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[163]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[164]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[165]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[166]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[167]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[168]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[169]);
            PressAnyKeyToContinueAndClear();

            // Wake up as an animal
            Reply(eventAndGoalExtraText[170]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[200]);
            PressAnyKeyToContinue();
        }

        // TODO MOVE LATER ^^^^

        static void HandleEat(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Thing is not in this location or in the inventory.
                    if (!ThingIsAvailable(thingId))
                    {
                        // Not here.
                        Reply(ThingsData[thingId].Answers[2]);
                        return;
                    }

                    // Thing is available.
                    switch (thingId)
                    {
                        case ThingId.Fish:
                        case ThingId.Honey:
                        case ThingId.Berries:
                        case ThingId.Mushrooms:
                        case ThingId.Nuts:
                        case ThingId.Trash:
                            EatTheThing(thingId);
                            return;

                        case ThingId.Flowers:
                            // If the honey puzzle is started allow player to eat flower otherwise no.
                            if (HoneyPuzzleStarted)
                            {
                                // If player hasn't already tried to eat flower.
                                if (!HaveTriedToEatFlower)
                                {
                                    // Text about eating a flower and then leaving the honey making to the bees.
                                    Reply(eventAndGoalExtraText[115]);
                                    HaveTriedToEatFlower = true;
                                }
                                else
                                {
                                    // Flowers are not tasty, no eat.
                                    Reply(eventAndGoalExtraText[116]);
                                }
                            }
                            else
                            {
                                // Says "Better not eat that." (if not changed).
                                Reply(eventAndGoalExtraText[114]);
                            }
                            return;

                        default:
                            if (words.Length > 1)
                            {
                                // Says "Better not eat that." (if not changed).
                                Reply(eventAndGoalExtraText[114]);
                            }
                            else
                            {
                                // Says "Maybe you should find something to eat." (if not changed).
                                Reply(eventAndGoalExtraText[117]);
                            }
                            return;
                    }
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // Says "Better not eat that." (if not changed).
                Reply(eventAndGoalExtraText[114]);
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

        static void HandleUse(string[] words)
        {
            // Getting a list of all ThingIds from words found in the command.
            List<ThingId> thingIdsFromCommand = GetThingIdsFromWords(words);

            // Getting a list of things as they are written in the entered command.
            List<string> thingKeysFromCommand = GetThingKeysFromWords(words, thingIdsFromCommand);

            // If there is any words that match any Thing IDs.
            if (thingKeysFromCommand.Count > 0)
            {
                // Checking every thing found in the command.
                foreach (string thing in thingKeysFromCommand)
                {
                    ThingId thingId = ThingIdsByName[thing];

                    // Thing is not in this location.
                    if (!ThingIsAvailable(thingId))
                    {
                        // Not here.
                        // Says "Can't do that" (if not changed).
                        Reply(eventAndGoalExtraText[26]);

                        return;
                    }

                    // Thing is one of these things and can eventually be used.
                    switch (thingId)
                    {
                        case ThingId.Binoculars:
                            UseBinoculars();
                            return;

                        case ThingId.FishingRodLong:
                        case ThingId.FishingRodOld:
                            UseFishingRod();
                            return;
                    }

                    // If the code makes it here, the thing can't be used.
                    // Says "Can't do that" (if not changed).
                    Reply(eventAndGoalExtraText[26]);
                }
            }
            // If there was no matching words and keys then the thing doesn't exist.
            else
            {
                // Says "Can't do that" (if not changed).
                Reply(eventAndGoalExtraText[26]);
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

                // Key word stick refers to the stick at current location.
                if (CurrentLocationId == LocationId.LeafyForestNorth)
                {
                    ThingIdsByName["stick"] = ThingId.LongStick;
                }
                else if (CurrentLocationId == LocationId.MossyForestNorth)
                {
                    ThingIdsByName["stick"] = ThingId.OldStick;
                }

                // Key word "flower" and "flowers" changes between meaning one flower and many.
                if (ThingIsAvailable(ThingId.Flower) || CurrentLocationId == LocationId.BeeForest)
                {
                    // Key word "flower" and "flowers" now means one flower.
                    ThingIdsByName["flower"] = ThingId.Flower;
                    ThingIdsByName["flowers"] = ThingId.Flower;
                }
                else
                {
                    // Key word "flower" and "flowers" now means many flowers.
                    ThingIdsByName["flower"] = ThingId.Flowers;
                    ThingIdsByName["flowers"] = ThingId.Flowers;
                }

                // Key word "bee" and "bees".
                if (ThingIsHere(ThingId.Bees) || ThingIsHere(ThingId.Bee))
                {
                    if (ThingIsHere(ThingId.Bees) && ThingIsHere(ThingId.Bee))
                    {
                        // When many bees and the lonely bee is at the same place, the words mean different bees.
                        ThingIdsByName["bee"] = ThingId.Bee;
                        ThingIdsByName["bees"] = ThingId.Bees;
                    }
                    else if (ThingIsHere(ThingId.Bees))
                    {
                        // If only many bees are at players location, the words mean many bees.
                        ThingIdsByName["bee"] = ThingId.Bees;
                        ThingIdsByName["bees"] = ThingId.Bees;
                    }
                    else
                    {
                        // If only lonely bees is at players location, the words mean lonely bee.
                        ThingIdsByName["bee"] = ThingId.Bee;
                        ThingIdsByName["bees"] = ThingId.Bee;
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

            // If the player has tried to make honey by eating a flower and the flower is still located at nowhere, move the honey to the den, as a gift from the bees.
            if (HaveTriedToEatFlower && ThingAt(ThingId.Honey, LocationId.Nowhere))
            {
                MoveThing(ThingId.Honey, LocationId.Nowhere, LocationId.Den);
            }

            // TODO change these conditions to depend on clearing all the previous puzzles but not the eating one (making it okey to eat the next day instead)
            if (GoalCompleted[Goal.EnoughFoodConsumed] && CurrentLocationId == LocationId.SouthEastForest && !GoalCompleted[Goal.HaveRelaxed])
            {
                // Start event about necklace.
                Relax();
            }


            /*
            if (!GoalCompleted[Goal.DreamtAboutShiftingShape])
            {
                // TODO
            }

            if (!GoalCompleted[Goal.EnoughFoodConsumed])
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
            }*/

            if (AllGoalsCompleted() || (GoalCompleted[Goal.DenMadeCozy] && GoalCompleted[Goal.DenCleaned] && HaveThing(ThingId.Fish) && HaveThing(ThingId.Honey)))
            {
                EndGame();
            }
        }
        #endregion

        #region Events
        static void EndGame()
        {
            Print("THE END! (That was all the puzzles that are finished right now)");
            quitGame = true;
            // TODO change text, and probably other things as well
        }

        // Events connected to cozy den goal.
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

        /// <summary>
        /// Text about completeing the cozy den goal.
        /// </summary>
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
            PressAnyKeyToContinue();

            DisplayNewLocation();
        }

        static void PickUpGrassAndGetFlower()
        {
            GetThing(ThingId.Grass);
            GetThing(ThingId.Flower);
            Reply(eventAndGoalExtraText[102]);
        }

        // Events about the leaves for cozy den goal.
        /// <summary>
        /// Drops all leaves in the pile if droped at the leafy forest (used for giving the ability to clear the pile).
        /// </summary>
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

        /// <summary>
        /// Add leaves to the leaf pile and the leaf pile to the inventory.
        /// </summary>
        /// <param name="thingId"></param>
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

        /// <summary>
        /// Removes the leaf pile from inventory and clears the leaf list.
        /// </summary>
        static void LoseLeafPile()
        {
            LoseThing(ThingId.PileOfLeaves);
            ThingsInPileOfLeaves.Clear();
        }

        /// <summary>
        /// Event text for losing leaves when going down the waterslide.
        /// </summary>
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

        /// <summary>
        /// Walking on the oath from leafy forest to den, if the leaves are in the correct order in the pile, player successfully brings the leaves with them.
        /// </summary>
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

        // Events about the fishing puzzle.
        static void StartFishingPuzzle()
        {
            if (!FishingPuzzleStarted)
            {
                // Start instructions for fishing puzzle.
                FishingPuzzleStarted = true;
                Reply(eventAndGoalExtraText[97]);
                PressAnyKeyToContinue();
                Reply(eventAndGoalExtraText[98]);
                PressAnyKeyToContinue();
            }
            else
            {
                // You cant get it, need to find a way to do it like the humans do.
                Reply(eventAndGoalExtraText[100]);
            }
        }

        static void UseFishingRod()
        {
            if (CurrentLocationId == LocationId.EastRiver)
            {
                Console.Clear();

                // Data about the long rod as default.
                ThingId fishingRod = ThingId.FishingRodLong;
                ThingId stick = ThingId.LongStick;

                // Change to data aboit the old rod if thats what the player has.
                if (HaveThing(ThingId.FishingRodOld))
                {
                    fishingRod = ThingId.FishingRodOld;
                    stick = ThingId.OldStick;
                }

                // The name of the fishing rod.
                string[] stickName = new string[] { ThingsData[stick].Name.ToLower() };

                // Text about starting to fish, sit down with you rod made out of choosen stick.
                InsertKeyWordAndDisplay(eventAndGoalExtraText[81], stickName);
                PressAnyKeyToContinue();
                Reply(eventAndGoalExtraText[82]);
                PressAnyKeyToContinue();

                if (fishingRod == ThingId.FishingRodLong)
                {
                    // You wait and wait and wait, nothing happens.
                    Reply(eventAndGoalExtraText[83]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[84]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[85]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[83]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[101]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[101]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[101]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[86]);
                    PressAnyKeyToContinue();
                }
                else if (fishingRod == ThingId.FishingRodOld)
                {
                    // Gets a fish on the hook but the rod breakes.
                    Reply(eventAndGoalExtraText[87]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[88]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[89]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[90]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[91]);
                    PressAnyKeyToContinue();
                    Reply(eventAndGoalExtraText[92]);
                    PressAnyKeyToContinue();
                }

                // Text about catching fish and throwing rod away.
                Reply(eventAndGoalExtraText[93]);
                PressAnyKeyToContinue();
                GetOneAndLoseOneThing(ThingId.Fish, fishingRod);

                DisplayNewLocation();
            }
            else
            {
                //Says "There is no good spots for fishing here." (if not changed)
                Reply(eventAndGoalExtraText[96]);
            }
        }

        // Trash.
        static void LookAtTrash()
        {
            if (ThingIsAvailable(ThingId.Trash))
            {
                // If player has trash in the inventory and there is no trash on the ground, display a message.
                if (HaveThing(ThingId.Trash) && !ThingIsHere(ThingId.Trash))
                {
                    // If the player have one pice of trash.
                    if (PileOfTrash.Count() == 1)
                    {
                        // Says "You are carrying some trash." (if not changed).
                        Reply(eventAndGoalExtraText[56]);
                    }
                    // If the player have more the 1 trash.
                    else
                    {
                        // Amount of trash.
                        string[] number = new string[] { PileOfTrash.Count().ToString() };

                        // Text about how much trash the player have.
                        InsertKeyWordAndDisplay(eventAndGoalExtraText[51], number);
                    }
                }
                // If there is trash on the current location but not in players inventory, display message.
                else if (ThingIsHere(ThingId.Trash) && !HaveThing(ThingId.Trash))
                {
                    // There is trash to see at the view point, in the trash cans. Special text.
                    if (CurrentLocationId == LocationId.ViewPoint)
                    {
                        // Message about the mess humans leave behind.
                        Reply(eventAndGoalExtraText[54]);
                    }
                    // For any other location.
                    else
                    {
                        // Says "There is trash on the ground, who put that there?" (if not changed).
                        Reply(eventAndGoalExtraText[52]);
                    }
                }
                // If both, display another message.
                else
                {
                    if (CurrentLocationId == LocationId.ViewPoint)
                    {
                        // Combination of how much trash player is carrying and trash on messy view point message.
                        string[] number = new string[] { PileOfTrash.Count().ToString() };
                        if (PileOfTrash.Count > 1)
                        {
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[51] + " " + eventAndGoalExtraText[55], number);
                        }
                        else
                        {
                            Reply(eventAndGoalExtraText[56] + " " + eventAndGoalExtraText[55]);
                        }

                    }
                    else
                    {
                        // Combination of how much trash player is carrying and trash on ground message.
                        string[] number = new string[] { PileOfTrash.Count().ToString() };
                        if (PileOfTrash.Count > 1)
                        {
                            InsertKeyWordAndDisplay(eventAndGoalExtraText[51] + " " + eventAndGoalExtraText[53], number);
                        }
                        else
                        {
                            Reply(eventAndGoalExtraText[56] + " " + eventAndGoalExtraText[53]);
                        }
                    }
                }
            }
            // There is no trash here.
            else
            {
                // Says "There is no trash around here, that's good!" (if not changed).
                Reply(ThingsData[ThingId.Trash].Answers[2]);
            }
        }

        static void PickUpTrash()
        {
            // Add trash to the pile.
            PileOfTrash.Add(ThingId.Trash);

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

            // If the player is picking up trash from any other location the view point, remove the trash from that location.
            if (CurrentLocationId != LocationId.ViewPoint)
            {
                RemoveThingFromLocation(ThingId.Trash);
            }
        }

        static void DropTrash()
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
            if (PiecesOfTrashCollected >= 10 && HaveThing(ThingId.Rope))
            {
                // Player is carrying enough trash and have the rope. Says "You didn't find anything." (if not changed).
                Reply(eventAndGoalExtraText[40]);
                return;
            }
            else if (PiecesOfTrashCollected >= 2 && !HaveThing(ThingId.Rope))
            {
                // Chance of getting rope.
                var random = new Random();
                int chanceForRope = random.Next(0, 10 - Math.Min(PileOfTrash.Count(), 9));

                if (chanceForRope == 0)
                {
                    // Player gets a rope.
                    GetThing(ThingId.Rope);
                    Reply(ThingsData[ThingId.Rope].Answers[0]);
                }
                else
                {
                    // Pick up trash.
                    PickUpTrash();
                    PiecesOfTrashCollected++;
                }
            }
            else
            {
                // Pick up trash.
                PickUpTrash();
                PiecesOfTrashCollected++;
            }
        }

        // Binoculars and nail.
        static void UseBinoculars()
        {
            string newWords = "";
            bool newInput = true;
            bool isValidInput = false;

            Console.Clear();
            // Instructions for using binoculars
            Reply(eventAndGoalExtraText[57]);

            // As long as the entered arrows are less then the number of arrows in the correct answer, ask for input.
            while (EnteredArrows.Count() < CorrectArrows.Count())
            {
                // Ask for input.
                if (newInput)
                {
                    newInput = false;
                    isValidInput = false;

                    // Display the narrative.
                    Console.ForegroundColor = NarrativeColor;
                    Print(eventAndGoalExtraText[62]);

                    // Line where player writes.
                    Console.ForegroundColor = PromptColor;
                    Console.Write("> ");
                }

                // Get the next key that is pressed.
                var key = Console.ReadKey().Key;

                // If player press an arrow key.
                if (key == ConsoleKey.LeftArrow || key == ConsoleKey.RightArrow || key == ConsoleKey.UpArrow || key == ConsoleKey.DownArrow)
                {
                    // Move binoculars.
                    string word = key.ToString().Remove(key.ToString().Length - 5);
                    MoveBinoculars(word.ToLower());
                    newInput = true;
                }
                // If player pressed anything else.
                else
                {
                    // If enter was pressed the input is done.
                    if (key == ConsoleKey.Enter)
                    {
                        // Asks for a command and splits it into seperate words, but also checks for matching double words and combines them.
                        string[] words = SplitCommand(newWords.ToLower());

                        // Clear words in preperation for next input.
                        newWords = "";

                        // Check if any words are valid inputs.
                        foreach (string word in words)
                        {
                            // If the word is a valid direction.
                            if (CorrectArrows.Contains(word))
                            {
                                // Move the camera and break out of this else statement.
                                MoveBinoculars(word);
                                isValidInput = true;
                                newInput = true;
                            }
                            else
                            {
                                // If the words contains an invalid word, go to invalid input message and stop looking throug the words.
                                isValidInput = false;
                                break;
                            }
                        }

                        // At least one input was not valid, "exit" binoculars.
                        if (!isValidInput)
                        {
                            // Message from the binoculars about invalid input, "quit" the event and display the current location instead.
                            Console.WriteLine();
                            Reply(eventAndGoalExtraText[63]);

                            // Clear the previous entered arrows.
                            EnteredArrows.Clear();
                            PressAnyKeyToContinue();

                            // Clear and display location description ("exit" binoculars).
                            DisplayNewLocation();
                            return;
                        }
                    }
                    // If space was pressed add a space instead of text ("Spacebar") to words.
                    else if (key == ConsoleKey.Spacebar)
                    {
                        newWords += " ";
                    }
                    // If backspace is pressed, remove tha last input.
                    else if (key == ConsoleKey.Backspace)
                    {
                        newWords = newWords.Remove(newWords.Length - 1);
                        Console.CursorLeft--;
                        Console.Write(" ");
                        Console.CursorLeft--;
                    }
                    // Add input to words.
                    else
                    {
                        newWords += key;
                    }
                }
            }

            // Check if the entered sequence is correct or not, when the number of entered directions are the same as in the correct answer.
            if (EnteredArrows.SequenceEqual(CorrectArrows))
            {
                // MADE IT!
                // Different if the hidden path was found before or not.
                if (!HiddenPathFound)
                {
                    // Add connection between waterfall and old tree, now it's possible to move to old tree from waterfall.
                    LocationsData[LocationId.Waterfall].Directions[Direction.North] = LocationId.SouthEastForest;

                    // The path is found!!
                    HiddenPathFound = true;

                    // Text about seeing something in the forest.
                    Reply(eventAndGoalExtraText[65]);
                }
                else
                {
                    // Text about the hidden path.
                    Reply(eventAndGoalExtraText[66]);
                }
                // TODO add extra description to old tree and waterfall about hidden path

                // Clear the entered arrows.
                EnteredArrows.Clear();
            }
            else
            {
                // Message abot reached limit of inputs, "quit" the event and display the current location instead.
                Reply(eventAndGoalExtraText[64]);
                EnteredArrows.Clear();
            }

            // "Exit" binoculars. Clear and show description about location.
            PressAnyKeyToContinue();
            DisplayNewLocation();
        }

        static void MoveBinoculars(string word)
        {
            // Add movement to list.
            EnteredArrows.Add(word);

            Console.CursorLeft = 0;
            Console.WriteLine("> " + word.ToString().PadRight(Console.WindowWidth - word.Length - 3 - 1, ' '));

            // Display message about movement.
            if (word.Contains("left"))
            {
                // Move binoculars left.
                Reply(eventAndGoalExtraText[58]);
            }
            else if (word.Contains("right"))
            {
                // Move binoculars right.
                Reply(eventAndGoalExtraText[59]);
            }
            else if (word.Contains("up"))
            {
                // Move binoculars up.
                Reply(eventAndGoalExtraText[60]);
            }
            else if (word.Contains("down"))
            {
                // Move binoculars down.
                Reply(eventAndGoalExtraText[61]);
            }
        }

        static void FindNail()
        {
            // Add the connection fron old tree to waterfall.
            LocationsData[LocationId.SouthEastForest].Directions[Direction.South] = LocationId.Waterfall;

            NailFound = true;
            GetThing(ThingId.Nail);

            // Text about almost stepping on a rusty nail.
            Console.Clear();
            Reply(eventAndGoalExtraText[71]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[72]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[73]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[74]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[75]);
            PressAnyKeyToContinue();

            MovePlayerToNewLocation(LocationId.SouthEastForest);
        }

        static void CombineToFishingRod()
        {
            // Text about having an idea!
            Reply(eventAndGoalExtraText[78]);
            PressAnyKeyToContinueAndClear();

            // Text about combinin nail and rope.
            Reply(eventAndGoalExtraText[79]);

            // Use the nail and rope.
            LoseThing(ThingId.Nail);
            LoseThing(ThingId.Rope);

            ThingId choosenStick = ThingId.Placeholder;
            // Use one of the sticks.
            if (HaveThing(ThingId.LongStick) && HaveThing(ThingId.OldStick))
            {
                while (choosenStick == ThingId.Placeholder)
                {
                    // Have booth so ask witch one to use.
                    choosenStick = AskWitchStick();

                    // Extra text for choosing again.
                    if (choosenStick == ThingId.Placeholder)
                    {
                        Reply(eventAndGoalExtraText[127]);
                    }
                }
            }
            // Only have long stick, use it.
            else if (HaveThing(ThingId.LongStick))
            {
                choosenStick = ThingId.LongStick;
            }
            // Only have old stick, use it.
            else if (HaveThing(ThingId.OldStick))
            {
                choosenStick = ThingId.OldStick;
            }

            LoseThing(choosenStick);

            // Get the name of the choosen stick.
            string[] choosenStickName = new string[] { ThingsData[choosenStick].Name.ToLower() };

            // Text about making fishing rod.
            InsertKeyWordAndDisplay(eventAndGoalExtraText[80], choosenStickName);

            // Get the fishing rod based on witch stick was used.
            if (choosenStick == ThingId.OldStick)
            {
                // Old.
                GetThing(ThingId.FishingRodOld);
                // Would like a better way for this to avoid hard coded text, maybe look for values and change them?
                ThingIdsByName["rod"] = ThingId.FishingRodOld;
                ThingIdsByName["fishing rod"] = ThingId.FishingRodOld;
            }
            else if (choosenStick == ThingId.LongStick)
            {
                // Long.
                GetThing(ThingId.FishingRodLong);
                // Would like a better way for this to avoid hard coded text, maybe look for values and change them?
                ThingIdsByName["rod"] = ThingId.FishingRodLong;
                ThingIdsByName["fishing rod"] = ThingId.FishingRodLong;
            }

            PressAnyKeyToContinue();
            DisplayNewLocation();
        }

        static void LookAtStick(string[] words)
        {
            // If player already specified witch stick to look at and that stick is here.
            if ((words.Contains("old stick") && ThingIsAvailable(ThingId.OldStick)) || (words.Contains("long stick") && ThingIsAvailable(ThingId.LongStick)))
            {
                if (words.Contains("old stick"))
                {
                    // Look at old stick.
                    Reply(ThingsData[ThingId.OldStick].Description);
                }
                else
                {
                    // Look at long stick.
                    Reply(ThingsData[ThingId.LongStick].Description);
                }
            }
            // If both sticks are around, ask witch one they mean.
            else if (ThingIsAvailable(ThingId.OldStick) && ThingIsAvailable(ThingId.LongStick))
            {
                ThingId choosenStick = AskWitchStick();

                if (choosenStick == ThingId.Placeholder)
                {
                    return;
                }

                Reply(ThingsData[choosenStick].Description);
            }
            else if (ThingIsAvailable(ThingId.OldStick))
            {
                // Look at old stick.
                Reply(ThingsData[ThingId.OldStick].Description);
            }
            else if (ThingIsAvailable(ThingId.LongStick))
            {
                // Look at long stick.
                Reply(ThingsData[ThingId.LongStick].Description);
            }
            else
            {
                // Says "There is no such thing to look at." (if not changed).
                Reply(eventAndGoalExtraText[46]);
            }
        }

        // Events about honey and bees.
        /// <summary>
        /// Checks if bee needs to be move when player goes to a new location.
        /// </summary>
        static void MoveBee(LocationId newLocation)
        {
            // Move the bee if the previous location had the bee and player have the flower.
            if (ThingsCurrentLocations[ThingId.Bee].Contains(CurrentLocationId) && HaveThing(ThingId.Flower))
            {
                MoveThing(ThingId.Bee, CurrentLocationId, newLocation);
            }

            // If the bee gets to the bee forest, change the bool that is used to display the correct text.
            if (ThingsCurrentLocations[ThingId.Bee].Contains(LocationId.BeeForest))
            {
                BeeHasBeenInBeeForest = true;
            }
        }

        static void DropFlower()
        {
            if (CurrentLocationId == LocationId.BeeForest)
            {
                // Drop flower.
                DropThing(ThingId.Flower);

                // Different text depending on if the bee is there or not.
                if (ThingsCurrentLocations[ThingId.Bee].Contains(LocationId.BeeForest))
                {
                    // Bee joins the other bees around the droped flower.
                    Reply(ThingsData[ThingId.Flower].Answers[4] + " " + eventAndGoalExtraText[108]);

                    // Can't pick up flower anymore.
                    ThingsYouCanGet.Remove(ThingId.Flower);

                    // If the flower is dropped at the bee forest when bee is there, bee is home (and stays home).
                    BeeIsHome = true;
                }
                else
                {
                    // Normal drop with text about bees liking the flower.
                    Reply(ThingsData[ThingId.Flower].Answers[4]);
                }
            }
            else
            {
                // NO drop.
                Reply(ThingsData[ThingId.Flower].Answers[5]);
            }
        }

        static void GetIdeaAboutHoney()
        {
            Console.Clear();

            // Text about having an idea to eat flowers to help make honey.
            Reply(eventAndGoalExtraText[110]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[111]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[112]);
            PressAnyKeyToContinue();

            HoneyPuzzleStarted = true;

            // Changing the current location to the new location and displaying the new location information.
            MovePlayerToNewLocation(LocationId.Glade);
        }

        //Events for swiming over river
        static void SwimOverRiver()
        {
            if (HaveTriedToSwimOverRiver)
            {
                // Add if list of things eaten is long enough, can swim, else no swim
                // TODO decide on this number v
                if (ListOfEatenThings.Count > 4 /* have eaten enough*/)
                {
                    GoalCompleted[Goal.EnoughFoodConsumed] = true;

                    // If the player have slept, they can swim over to the other side of the river.
                    if (DidSleep)
                    {
                        Console.Clear();

                        // Text about being ready to swim.
                        Reply(eventAndGoalExtraText[119]);
                        PressAnyKeyToContinue();

                        // Move player to the new location and displays description.
                        MovePlayerToNewLocation(LocationId.Cliffs);
                    }
                    else
                    {
                        // It's getting late, should go relax a bit and then sleep.
                        Reply(eventAndGoalExtraText[120]);
                    }
                }
                else
                {
                    // Bear needs to eat before going for a swim.
                    Reply(eventAndGoalExtraText[121]);
                }
            }
            else if (!HaveTriedToSwimOverRiver)
            {
                // Calls the start of the swim puzzle, is only called once.
                StartSwimPuzzle();
            }
        }

        static void StartSwimPuzzle()
        {
            Console.Clear();

            // Text about wanting to swim over river but needing to eat first.
            Reply(eventAndGoalExtraText[122]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[123]);
            PressAnyKeyToContinue();

            // Make things to eat appear and change puzzle to started.
            MoveThing(ThingId.Berries, LocationId.Nowhere, LocationId.LeafyForestSouth);
            MoveThing(ThingId.Mushrooms, LocationId.Nowhere, LocationId.MossyForestSouth);
            MoveThing(ThingId.Nuts, LocationId.Nowhere, LocationId.Glade);
            HaveTriedToSwimOverRiver = true;

            // Display the river location to player again.
            DisplayNewLocation();
        }

        static void EatTheThing(ThingId thingId)
        {
            // Check if the thing was already eaten or not.
            if (ListOfEatenThings.Contains(thingId))
            {
                // Text about player have already eaten that.
                string[] thing = new string[] { ThingsData[thingId].Name.ToLower() };
                InsertKeyWordAndDisplay(eventAndGoalExtraText[130], thing);
            }
            else
            {
                // Add the thing to eaten things and display a message.
                ListOfEatenThings.Add(thingId);
                string[] thing = new string[] { ThingsData[thingId].Name.ToLower() };
                InsertKeyWordAndDisplay(eventAndGoalExtraText[129], thing);
            }

            // If the player has eaten more then one thing, tell the player all the things they already ate.
            if (ListOfEatenThings.Count > 1)
            {
                var eatenThings = new List<string>();

                // Get the name of all eaten things.
                foreach (ThingId thing in ListOfEatenThings)
                {
                    eatenThings.Add(ThingsData[thing].Name.ToLower());
                }

                // Format the list.
                eatenThings[0] = FormatListIntoString(eatenThings);

                // Display all that is eaten.
                Reply(eventAndGoalExtraText[128] + " " + eatenThings[0] + ".");
            }
        }

        // Events about necklace.
        static void Relax()
        {
            GoalCompleted[Goal.HaveRelaxed] = true;

            // You are at the old tree, time to relax!
            Reply(eventAndGoalExtraText[136]);
            PressAnyKeyToContinueAndClear();

            // Text about relaxing and thinking about the old stories, the beeing interupted by seeing something glimmer in the sun
            Reply(eventAndGoalExtraText[137]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[138]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[139]);
            PressAnyKeyToContinue();
            // TODO The old story.
            Reply(eventAndGoalExtraText[140]);
            PressAnyKeyToContinue();
            // Something disturbs you.
            Reply(eventAndGoalExtraText[141]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[142]);
            PressAnyKeyToContinue();

            // Add the necklace to this location (old tree).
            MoveThing(ThingId.Necklace, ThingsCurrentLocations[ThingId.Necklace][0], CurrentLocationId);

            // Display the location again (now with extra description about necklace).
            DisplayNewLocation();
        }

        static void FindNecklace()
        {
            // You take a closer look in the bush and find a necklace.
            Reply(eventAndGoalExtraText[144]);
            PressAnyKeyToContinueAndClear();

            // Text about the necklace, putting it on, trying to shift shape.
            Reply(eventAndGoalExtraText[145]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[146]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[147]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[148]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[149]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[150]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[151]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[150]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[150]);
            PressAnyKeyToContinue();
            Reply(eventAndGoalExtraText[152]);
            PressAnyKeyToContinue();
            // That didnt end very relaxing, but now the sun is almost completely down, time to sleep.
            Reply(eventAndGoalExtraText[153]);
            PressAnyKeyToContinue();

            // Pick up the necklace.
            MoveThing(ThingId.Necklace, CurrentLocationId, LocationId.Inventory);

            DisplayNewLocation();
        }

        // Other events.
        static void MovePlayerToNewLocation(LocationId newLocationId)
        {
            // Changing the current location to the new location and displaying the new location information.
            CurrentLocationId = newLocationId;
            DisplayNewLocation();
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
            AddExtraDescription();
            Console.WriteLine();

            /*
            // TODO For testing purposes vvvvvvvv

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

            // TODO For testing purposes ^^^^^^^^
            */
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

        /// <summary>
        /// Checks id extra description is needed after a normal location description, and diaplays.
        /// </summary>
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

                if (ThingIsHere(ThingId.Honey))
                {
                    // TODO color
                    Console.WriteLine();

                    Print(eventAndGoalExtraText[118]);
                }
            }

            // Add extra text if the hidden path is found. At location waterfall.
            if (!NailFound && CurrentLocationId == LocationId.Waterfall)
            {
                // TODO color
                Console.WriteLine();

                // When hidden path is not found.
                if (!HiddenPathFound)
                {
                    Print(eventAndGoalExtraText[67]);
                }
                // When path is found but not walked.
                else
                {
                    // "What did I see in the north?"
                    Print(eventAndGoalExtraText[68]);
                }
            }
            // Hidden path is walked on.
            else if (NailFound && (CurrentLocationId == LocationId.Waterfall || CurrentLocationId == LocationId.SouthEastForest))
            {
                // TODO color
                Console.WriteLine();

                // Add text for description about connection between waterfall and old tree.
                // At location waterfall.
                if (CurrentLocationId == LocationId.Waterfall)
                {
                    Print(eventAndGoalExtraText[69]);
                }
                // For old tree in south east forest.
                else if (CurrentLocationId == LocationId.SouthEastForest)
                {
                    Print(eventAndGoalExtraText[70]);
                }
            }

            // About the bee and the flower.
            if (ThingIsHere(ThingId.Bee))
            {
                // TODO color
                Console.WriteLine();

                if (CurrentLocationId == LocationId.BeeForest)
                {
                    if (HaveThing(ThingId.Flower))
                    {
                        // Text about bee making it back home.
                        Reply(eventAndGoalExtraText[106]);
                    }
                }
                else
                {
                    // Different text depending on if the player have the flower.
                    if (HaveThing(ThingId.Flower))
                    {
                        // Text for if the bee has been in the bee forest but player didn't drop the flower and bee is still following.
                        if (BeeHasBeenInBeeForest)
                        {
                            // Bee didn't stay home, seems to like your flower too much.
                            Reply(eventAndGoalExtraText[107]);
                        }
                        // Text about bee following.
                        else
                        {
                            // Different text depending on the location, bees starting location or not.
                            if (CurrentLocationId == ThingsData[ThingId.Bee].StartingLocationId[0])
                            {
                                // Says "There is a lonely bee flying around, it looks lost, but it seems to like your flower." (if not changed)
                                Print(eventAndGoalExtraText[103]);
                            }
                            else
                            {
                                // Says "The bee follows you, it really likes you flower." (if not changed)
                                Print(eventAndGoalExtraText[104]);
                            }
                        }
                    }
                    // Don't have the flower.
                    else if (!HaveThing(ThingId.Flower))
                    {
                        // TODO should this be print and not reply?
                        // Says "There is a lonely bee flying around, it looks lost." (if not changed)
                        Print(eventAndGoalExtraText[105]);
                    }
                }
            }
            // Add extra description for flower on the ground in the bee forest.
            else if (CurrentLocationId == LocationId.BeeForest && ThingIsHere(ThingId.Flower))
            {
                // TODO color
                Console.WriteLine();

                // Text about bees liking the flower on the ground.
                Reply(eventAndGoalExtraText[109]);
            }

            // Description about idea about making honey.
            if (CurrentLocationId == LocationId.Glade && HoneyPuzzleStarted && !HaveTriedToEatFlower)
            {
                // TODO color
                Console.WriteLine();

                // Text about going to try to eat a flower.
                Reply(eventAndGoalExtraText[113]);
            }

            // If the current location have any of these things.
            if (ThingIsHere(ThingId.Berries))
            {
                // TODO color
                Console.WriteLine();

                // Berries.
                Reply(eventAndGoalExtraText[124]);
            }
            else if (ThingIsHere(ThingId.Mushrooms))
            {
                // TODO color
                Console.WriteLine();

                // Mushrooms.
                Reply(eventAndGoalExtraText[125]);
            }
            else if (ThingIsHere(ThingId.Nuts))
            {
                // TODO color
                Console.WriteLine();

                // Nuts.
                Reply(eventAndGoalExtraText[126]);
            }

            if (ThingIsHere(ThingId.Necklace))
            {
                // TODO color
                Console.WriteLine();

                // Extra discription about necklace in the busches.
                Reply(eventAndGoalExtraText[143]);
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

        /// <summary>
        /// Takes a list and gives back a string with all the items from the list, formatted with "," and "and".
        /// </summary>
        /// <param name="things"></param>
        /// <returns></returns>
        static string FormatListIntoString(List<string> things)
        {
            // If there is more then one thing in the players inventory, format all the things into one string.
            if (things.Count > 1)
            {
                // Add a new string that combines the last two things and adds "and" inbetween them.
                things.Add(things[things.Count - 2] + eventAndGoalExtraText[50] + things[things.Count - 1]);
                // Remove the two seperated words.
                things.RemoveRange(things.Count - 3, 2);

                // If there is more things then 2, add "," between all of them but the last two which are already combined with "and".
                if (things.Count > 1)
                {
                    // Join all words together in a string.
                    string joinList = String.Join(", ", things);
                    // Remove all things in the list.
                    things.Clear();
                    // Add the formatted string with all the things to the list.
                    things.Add(joinList);
                }
            }
            return things[0].ToLower();
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
        /// Changes a certain things location from a location to a location.
        /// </summary>
        /// <param name="thingId"></param>
        /// <param name="fromLocation"></param>
        /// <param name="toLocation"></param>
        static void MoveThing(ThingId thingId, LocationId fromLocation, LocationId toLocation)
        {
            // Removes the thing from a location.
            ThingsCurrentLocations[thingId].Remove(fromLocation);
            // Adds the thing to a new location.
            ThingsCurrentLocations[thingId].Add(toLocation);
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
        /// Adds a certain thing to the inventory (without removing it from any place).
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
            LoseThing(thingId);
            AddThingToLocation(thingId);
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
        /// Removes a thing from the current location.
        /// </summary>
        /// <param name="thingId"></param>
        static void RemoveThingFromLocation(ThingId thingId)
        {
            ThingsCurrentLocations[thingId].Remove(CurrentLocationId);
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

        // TODO finding necklace
        // TODO text about trying necklace and dream
        // TODO end of part 1
    }
}
