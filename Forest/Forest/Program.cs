using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Forest
{
    class Program
    {
        const ConsoleColor NarrativeColor = ConsoleColor.Gray;
        const ConsoleColor PromptColor = ConsoleColor.White;
        const int PrintPauseMilliseconds = 150;

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

        static void Main(string[] args)
        {
            // Displaying title art
            string titleArtPath = "ForestTitleArt.txt";
            string[] title = File.ReadAllLines(titleArtPath);
            foreach (string line in title)
            {
                Console.WriteLine(line);
            }
            Console.ReadKey();
            Console.Clear();

            // Displaying the introduction/first part of the games story
            string storyPath = "ForestGameStory.txt";
            string[] gameStory = File.ReadAllLines(storyPath);
            Print(gameStory[0]);
        }
    }
}
