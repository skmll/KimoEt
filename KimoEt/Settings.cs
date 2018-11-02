using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace KimoEt
{
    public class Settings
    {
        public static Settings Instance;

        public static readonly int RATING_COLOR_NONE = 0;
        public static readonly int RATING_COLOR_DEFAULT = 1;
        public static readonly int RATING_COLOR_USER = 2;

        public static readonly int USE_TDC_TIER_LIST = 0;
        public static readonly int USE_SUNYVEIL_TIER_LIST = 1;
        public static readonly int USE_BOTH_TIER_LIST = 2;
        
        public int RatingColorMode { get; set; }
        public int TierListMode { get; set; }
        public List<string> RatingColorUserModeColors { get; set; }
        public System.Windows.Point MenuPoint { get; set; }
        public System.Windows.Point RatingColorsRulerPoint { get; set; }
        public decimal LastVersionUsed { get; set; }

        private static Settings BrandNewSettings()
        {
            return new Settings()
            {
                RatingColorMode = RATING_COLOR_DEFAULT,
                TierListMode = USE_TDC_TIER_LIST,
                RatingColorUserModeColors = new List<string>(RatingsColorsDefault),
                MenuPoint = new System.Windows.Point(1300, 115),
                RatingColorsRulerPoint = new System.Windows.Point(50, 50),
                LastVersionUsed = 0m
            };
        }

        public static void SaveSettingsToDisk()
        {
            // Write the contents of the variable settings to a file.
            WriteToJsonFile<Settings>(@"settings.json", Instance);
        }

        public static void LoadSettingsFromDisk()
        {
            // Read the file contents back into a variable.
            Instance = ReadFromJsonFile(@"settings.json");
        }

        public void SetNewMenuPosition(System.Windows.Point point)
        {
            MenuPoint = point;
            SaveSettingsToDisk();
        }

        public void SetNewLastVersionUsed(decimal version)
        {
            LastVersionUsed = version;
            SaveSettingsToDisk();
        }

        public void SetNewRatingColorsRulerPosition(System.Windows.Point point)
        {
            RatingColorsRulerPoint = point;
            SaveSettingsToDisk();
        }

        public void SetNewColorModeString(string text)
        {
            if (text == "User")
            {
                RatingColorMode = RATING_COLOR_USER;
            }
            else if (text =="Default")
            {
                RatingColorMode = RATING_COLOR_DEFAULT;
            }
            else
            {
                RatingColorMode = RATING_COLOR_NONE;
            }
            SaveSettingsToDisk();
        }

        public void SetNewTierListString(string text)
        {
            if (text == "TDC's")
            {
                TierListMode = USE_TDC_TIER_LIST;
            }
            else if (text == "Sunyveil's")
            {
                TierListMode = USE_SUNYVEIL_TIER_LIST;
            }
            else
            {
                TierListMode = USE_BOTH_TIER_LIST;
            }
            SaveSettingsToDisk();
        }

        public void SetNewColorForRating(decimal rating, string newColor)
        {
            RatingColorUserModeColors[RatingsColorsThresholds.IndexOf(rating)] = newColor;
            RatingColorMode = RATING_COLOR_USER;
            SaveSettingsToDisk();
        }

        public List<string> GetChosenRatingsColors()
        {
            var colorsToUse = RatingsColorsDefault;

            if (RatingColorMode == RATING_COLOR_NONE)
            {
                colorsToUse = RatingsColorsNone;
            }
            else if (RatingColorMode == RATING_COLOR_USER)
            {
                colorsToUse = RatingColorUserModeColors;
            }

            return colorsToUse;
        }

        public static List<Color> GetAllAvailbleColors()
        {
            var colors = new List<Color>();

            foreach (var color in ColorStrings)
            {
                var colorFinal = Color.FromName(color);
                colors.Add(colorFinal);
            }

            colors.Sort((Comparison<Color>)(
              (Color left, Color right) =>
                 (left.R * 299 + left.G * 587 + left.B * 114).CompareTo(right.R * 299 + right.G * 587 + right.B * 114)
              ));

            return colors;
        }


        public static readonly List<string> RatingsColorsNone = new List<string>()
        {
            "White",
            "White",
            "White",
            "White",
            "White"
        };

        public static readonly List<string> RatingsColorsDefault = new List<string>()
        {
            "Red",
            "Yellow",
            "Green",
            "Cyan",
            "White" 
        };

        public static readonly List<decimal> RatingsColorsThresholds = new List<decimal>()
        {
            0m,
            1.6m,
            3.9m,
            4m,
            5m,
        };

        public static readonly List<string> ColorStrings = new List<string>() {
                "Black",
                "Blue",
                "DarkRed",
                "Purple",
                "DarkGreen",
                "DarkViolet",
                "Green",
                "Red",
                "Crimson",
                "MediumVioletRed",
                "BlueViolet",
                "ForestGreen",
                "DarkCyan",
                "DeepPink",
                "Magenta",
                "SeaGreen",
                "OrangeRed",
                "Gray",
                "DodgerBlue",
                "DarkGoldenrod",
                "LimeGreen",
                "DeepSkyBlue",
                "Lime",
                "DarkOrange",
                "SpringGreen",
                "Goldenrod",
                "Orange",
                "Cyan",
                "SkyBlue",
                "Gold",
                "GreenYellow",
                "Yellow",
                "White"
        };

        /// <summary>
        /// Writes the given object instance to a Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
        /// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [JsonIgnore] attribute.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite, Formatting.Indented);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Reads an object instance from an Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the Json file.</returns>
        public static Settings ReadFromJsonFile(string filePath) //where Settings : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Settings>(fileContents);
            }
            catch (Exception)
            {
                return BrandNewSettings();
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
    }
}
