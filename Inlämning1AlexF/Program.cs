using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Assignment1
{

    public class AppDbContext : DbContext
    {
        public DbSet<Movies> Movies { get; set; }
        public DbSet<Screenings> Screenings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DataAccessConsoleAssignment;Integrated Security=True;MultipleActiveResultSets=True;");
        }

    }
    public class Movies
    {
        [Required]
        public int ID { get; set; }

        [MaxLength(255)]
        [Required]
        public string Title { get; set; }

        [Column(TypeName = "Date")]
        public DateTime ReleaseDate { get; set; }

        public List<Screenings> screenings { get; set; }

    }
    public class Screenings
    {
        public int ID { get; set; }
        public DateTime DateTime { get; set; }
        public int MovieID { get; set; }
        public Int16 Seats { get; set; }
        public Movies Movie { get; set; }

    }


    public class Program
    {
        private static AppDbContext database;
        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            using (database = new AppDbContext())
            {
                bool running = true;

                while (running)
                {
                    int selected = Utils.ShowMenu("What do you want to do?", new[] {
                    "List Movies",
                    "Add Movie",
                    "Delete Movie",
                    "Load Movies from CSV File",
                    "List Screenings",
                    "Add Screening",
                    "Delete Screening",
                    "Exit"
                });
                    Console.Clear();

                    if (selected == 0) ListMovies();
                    else if (selected == 1) AddMovie();
                    else if (selected == 2) DeleteMovie();
                    else if (selected == 3) LoadMoviesFromCSVFile();
                    else if (selected == 4) ListScreenings();
                    else if (selected == 5) AddScreening();
                    else if (selected == 6) DeleteScreening();
                    else running = false;

                    Console.WriteLine();
                }
            }
        }

        public static void ListMovies()
        {
            if (database.Movies.Count() == 0)
            {
                Utils.WriteHeading("No Movies in database");
            }
            else
            {
                Utils.WriteHeading("Movies in database:");
                foreach (var m in database.Movies)
                {
                    Console.WriteLine(m.Title + " (" + m.ReleaseDate.Year + ")");
                }

            }
        }

        public static void AddMovie()
        {
            Utils.WriteHeading("Add New Movie");
            Movies movie = new Movies();
            movie.Title = Utils.ReadString("Title");
            movie.ReleaseDate = Utils.ReadDate("Date");

            database.Add(movie);
            database.SaveChanges();
            Utils.WriteHeading("Movie added");
        }

        public static void DeleteMovie()
        {
            if (database.Movies.Count() == 0)
            {
                Utils.WriteHeading("No Movies in database");
            }
            else
            {
                Utils.WriteHeading("Movies to Delete:");
                string[] movieNames = database.Movies.Select(m => m.Title).ToArray();
                int movieIndex = Utils.ShowMenu("Movies:", movieNames);
                string movieName = movieNames[movieIndex];
                var movie = database.Movies.First(m => m.Title == movieName);

                database.Remove(movie);
                database.SaveChanges();
                Utils.WriteHeading("Movie deleted");
            }

        }

        public static void LoadMoviesFromCSVFile()
        {
            {
                if (database.Movies.Count() != 0)
                {
                    string[] answer = new string[] { "Yes", "No" };
                    int answerIndex = Utils.ShowMenu("Clear Movies before Loading?", answer);
                    string questionAnswer = answer[answerIndex];
                    if (questionAnswer.Equals("Yes"))
                    {
                        foreach (var item in database.Movies)
                        {
                            database.Remove(item);

                        }
                        database.SaveChanges();
                        Utils.WriteHeading("Database Cleared");
                    }
                    else
                    {
                        return;
                    }


                }
                string path = Utils.ReadString("Chose filepath");
                string[] lines = File.ReadAllLines(path).ToArray();
                foreach (var l in lines)
                {

                    Movies movie = new Movies();
                    string[] values = l.Split(',').Select(v => v.Trim()).ToArray();
                    movie.Title = values[0];
                    movie.ReleaseDate = DateTime.Parse(values[1]);
                    database.Add(movie);
                    database.SaveChanges();

                }
                Utils.WriteHeading("Movies loaded Successfully");
            }
        }

        public static void ListScreenings()
        {
            if (database.Screenings.Count() == 0)
            {
                Utils.WriteHeading("No Screening in database");
            }
            else
            {
                Utils.WriteHeading("Screenings in database:");
                foreach (var s in database.Screenings.Include(a => a.Movie))
                {
                    Console.WriteLine(s.Movie.Title + " - " + s.DateTime);
                }
            }
        }

        public static void AddScreening()
        {
            if (database.Movies.Count() == 0)
            {
                Utils.WriteHeading("No Movies in database");
            }
            else
            {
                Utils.WriteHeading("Add Screening");
                Screenings screenings = new Screenings();
                string[] movieNames = database.Movies.Select(m => m.Title).ToArray();
                var screeningIndex = Utils.ShowMenu("Movie:", movieNames);
                string movieName = movieNames[screeningIndex];
                screenings.Movie = database.Movies.First(m => m.Title == movieName);



                var selectedDate = Utils.ReadFutureDate(movieName);
                var hr = Utils.ReadInt("Hour");
                var mn = Utils.ReadInt("Minute");

                DateTime date = selectedDate.AddHours(hr).AddMinutes(mn);
                screenings.DateTime = date;
                screenings.Seats = Int16.Parse(Utils.ReadString("Number of seats:"));


                database.Add(screenings);
                database.SaveChanges();
                Utils.WriteHeading("Screening added");

            }

        }

        public static void DeleteScreening()
        {
            if (database.Screenings.Count() == 0)
            {
                Utils.WriteHeading("No Screenings in database");
            }
            else
            {

                string[] screeningNames = database.Screenings.Include(a => a.Movie).Select(a => a.Movie.Title + " " +
                a.DateTime.Date + " " + a.DateTime.Hour + ":" + a.DateTime.Minute + " (" + a.Seats + " seats)").ToArray();
               
                int screeningIndex = Utils.ShowMenu("Movies:", screeningNames);
                string screeningName = screeningNames[screeningIndex];
               
                var artist = database.Screenings.Include(a => a.Movie).First(a => a.Movie.Title + " " +
                a.DateTime.Date + " " + a.DateTime.Hour + ":" + a.DateTime.Minute + " (" + a.Seats + " seats)" == screeningName);
                Utils.WriteHeading("Screening deleted");
                database.Remove(artist);
                database.SaveChanges();
            }

        }
    }



    public static class Utils
    {
        public static string ReadString(string prompt)
        {
            Console.Write(prompt + " ");
            string input = Console.ReadLine();
            return input;
        }

        public static int ReadInt(string prompt)
        {
            Console.Write(prompt + " ");
            int input = int.Parse(Console.ReadLine());
            return input;
        }

        public static DateTime ReadDate(string prompt)
        {
            Console.WriteLine(prompt);
            int year = ReadInt("Year:");
            int month = ReadInt("Month:");
            int day = ReadInt("Day:");
            var date = new DateTime(year, month, day);
            return date;
        }

        public static DateTime ReadFutureDate(string prompt)
        {
            var dates = new[]
            {
                DateTime.Now.Date,
                DateTime.Now.AddDays(1).Date,
                DateTime.Now.AddDays(2).Date,
                DateTime.Now.AddDays(3).Date,
                DateTime.Now.AddDays(4).Date,
                DateTime.Now.AddDays(5).Date,
                DateTime.Now.AddDays(6).Date,
                DateTime.Now.AddDays(7).Date
            };
            var wordOptions = new[] { "Today", "Tomorrow" };
            var nameOptions = dates.Skip(2).Select(d => d.DayOfWeek.ToString());
            var options = wordOptions.Concat(nameOptions);
            int daysAhead = ShowMenu(prompt, options.ToArray());
            var selectedDate = dates[daysAhead];
            return selectedDate;
        }

        public static void WriteHeading(string text)
        {
            string overline = new string('-', text.Length);
            Console.WriteLine(overline);
            Console.WriteLine(text);
            string underline = new string('-', text.Length);
            Console.WriteLine(underline);
        }

        public static int ShowMenu(string prompt, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty array of options.");
            }

            Console.WriteLine(prompt);

            int selected = 0;

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                // If this is not the first iteration, move the cursor to the first line of the menu.
                if (key != null)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.CursorTop - options.Length;
                }

                // Print all the options, highlighting the selected one.
                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    if (i == selected)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine("- " + option);
                    Console.ResetColor();
                }

                // Read another key and adjust the selected value before looping to repeat all of this.
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Length - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }
            }

            // Reset the cursor and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }
        public static int ShowMenu2(string prompt, DateTime[] options)
        {
            if (options == null || options.Length == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty array of options.");
            }

            Console.WriteLine(prompt);

            int selected = 0;

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                // If this is not the first iteration, move the cursor to the first line of the menu.
                if (key != null)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.CursorTop - options.Length;
                }

                // Print all the options, highlighting the selected one.
                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    if (i == selected)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine("- " + option);
                    Console.ResetColor();
                }

                // Read another key and adjust the selected value before looping to repeat all of this.
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Length - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }
            }

            // Reset the cursor and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }
    }
}


