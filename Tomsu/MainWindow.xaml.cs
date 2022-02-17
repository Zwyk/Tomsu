using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tomsu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Random rand = null;
        public string seed = null;
        public List<string> WORDS;

        public string RED_SQUARE = "🟥";
        public string YELLOW_SQUARE = "🟨";
        public string BLUE_SQUARE = "🟦";

        public bool Pause = false;

        public int TRIES = 6;

        public List<List<Border>> Boxes = null;
        public List<List<string>> Results = null;

        public string Word;
        public char FirstLetter;

        public char[] CurrentGuess;

        public Border CurrentBox = null;
        public TextBlock CurrentTxt = null;

        public int CurrentTry;
        public int CurrentChar;

        public int Guess = 1;

        public DateTime Start;

        public MainWindow()
        {
            InitializeComponent();

            WORDS = LoadWords();

            Play();
        }

        private void Play()
        {
            if (seed == null || seed != Seed.Text)
            {
                if (Seed.Text != "")
                {
                    seed = Seed.Text;
                    rand = new Random(seed.GetHashCode());
                }
                else
                {
                    Seed.Text = Math.Abs(Guid.NewGuid().GetHashCode()).ToString();
                    seed = Seed.Text;
                    Console.WriteLine(seed);
                    rand = new Random(seed.GetHashCode());
                }

                PlayButton.Content = "Next";

                GameText.Focus();

                Guess = 1;
                GuessNb.Content = Guess;
            }
            else
            {
                Guess++;
                GuessNb.Content = Guess;
            }

            Pause = false;

            if(Boxes != null)
            {
                foreach(var v in Boxes)
                {
                    foreach(var w in v)
                    {
                        Main.Children.Remove(w);
                    }
                }
            }

            Word = WORDS[rand.Next(WORDS.Count)];
            Console.WriteLine(Word);
            FirstLetter = Word[0];

            CurrentGuess = new char[Word.Length];
            CurrentGuess[0] = FirstLetter;
            for (int i = 1; i < Word.Length; i++) CurrentGuess[i] = '.';

            Boxes = new List<List<Border>>();
            CreateGameGrid(Word.Length);

            CurrentTry = 0;
            CurrentChar = 0;

            Results = new List<List<string>>();

            SetupTryBoxes();

            RefreshCurrent();

            Start = DateTime.Now;
        }

        private void GameText_KeyDown(object sender, KeyEventArgs e)
        {
            //Console.WriteLine(e.Key);
            if (!Pause)
            {
                if (e.Key == Key.Enter)
                {
                    string input = "";
                    foreach (Border b in Boxes[CurrentTry])
                    {
                        input += (b.Child as TextBlock).Text;
                    }

                    if (WORDS.Any(w => w == input))
                    {
                        Results.Add(new List<string>());
                        string done = "";
                        for (int i = 0; i < Word.Length; i++)
                        {
                            char c = input[i];
                            if (Word[i] == c)
                            {
                                CurrentGuess[i] = c;
                                Boxes[CurrentTry][i].Background = new SolidColorBrush(Color.FromRgb(231, 0, 42));
                                Results[CurrentTry].Add(RED_SQUARE);
                                done += input[i];
                            }
                            else
                            {
                                Boxes[CurrentTry][i].Background = new SolidColorBrush(Color.FromRgb(0, 119, 199));
                                Results[CurrentTry].Add(BLUE_SQUARE);
                            }
                        }
                        for (int i = 0; i < Word.Length; i++)
                        {
                            char c = input[i];
                            if (Word.Count(ch => ch == c) > done.Count(ch => ch == c) && CurrentGuess[i] != c)
                            {
                                Boxes[CurrentTry][i].Background = new SolidColorBrush(Color.FromRgb(255, 189, 0));
                                Results[CurrentTry][i] = YELLOW_SQUARE;
                            }
                        }

                        if (new string(CurrentGuess) == Word)
                        {
                            Pause = true;
                            ClipRes(true);
                        }
                        else
                        {
                            if (CurrentTry < TRIES - 1)
                            {
                                NextTry();
                            }
                            else
                            {
                                Boxes.Add(new List<Border>());

                                ClipRes(false);

                                for (int j = 0; j < Word.Length; j++)
                                {
                                    Border copy = Copy(BoxTemplate) as Border;
                                    Thickness t = copy.Margin;
                                    t.Left += (BoxTemplate.Width - 1) * j;
                                    t.Top += (BoxTemplate.Width - 1) * (TRIES + 1);
                                    copy.Margin = t;

                                    copy.Background = new SolidColorBrush(Color.FromRgb(231, 0, 42));
                                    (copy.Child as TextBlock).Text = Word[j].ToString();

                                    Main.Children.Add(copy);
                                    Boxes[TRIES].Add(copy);
                                }
                            }
                        }
                    }
                }
                else if (e.Key == Key.Back)
                {
                    if (CurrentChar == 0)
                    {
                        SetupTryBoxes();
                    }
                    else
                    {
                        CurrentTxt.Text = '.'.ToString();
                        NextChar(true);
                    }
                }
                else if (e.Key == Key.Left)
                {
                    NextChar(true);
                }
                else if (e.Key == Key.Right)
                {
                    NextChar();
                }
                else
                {
                    string c = e.Key == Key.Space ? " " : e.Key.ToString().ToUpper();
                    if (c.Length == 1)
                    {
                        if (CurrentChar == 0 && c != Word[0].ToString()) NextChar();
                        CurrentTxt.Text = c;
                        NextChar();
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                Play();
            }
        }

        private void ClipRes(bool won = true)
        {
            string clip = "TOMSU <" + seed + "> #" + Guess;
            clip += Environment.NewLine;
            foreach (var t in Results)
            {
                if (clip != "") clip += Environment.NewLine;
                foreach (var r in t)
                {
                    clip += r;
                }
            }
            clip += Environment.NewLine;
            clip += (won ? "SUCCESS " + CurrentTry + "/6" : "FAILED") + " in " + TimeSpanToStr(DateTime.Now - Start);
            clip += Environment.NewLine;
            clip += Environment.NewLine;
            clip += @"https://github.com/Zwyk/Tomsu/releases/download/v1/Tomsu.zip";
            Clipboard.SetText(clip);
        }

        private void NextChar(bool back = false)
        {
            if (back && CurrentChar > 0) CurrentChar--;
            else if (!back && CurrentChar < Word.Length - 1) CurrentChar++;
            RefreshCurrent();
        }

        private void NextTry()
        {
            if (CurrentTry < TRIES - 1)
            {
                CurrentTry++;
                CurrentChar = 0;
                RefreshCurrent(true);

                SetupTryBoxes();
            }
        }

        private void SetupTryBoxes()
        {
            for (int i = 0; i < Word.Length; i++)
            {
                (Boxes[CurrentTry][i].Child as TextBlock).Text = CurrentGuess[i].ToString();
            }
        }

        private void SelectBox(Border box, bool select = true)
        {
            if(select) box.Background = new SolidColorBrush(Color.FromRgb(0, 82, 137));
            else box.Background = new SolidColorBrush(Color.FromRgb(0, 119, 199));
        }

        private void RefreshCurrent(bool nextTry = false)
        {
            int tries = CurrentTry;
            int pos = CurrentChar;

            if (CurrentBox != null && !nextTry) SelectBox(CurrentBox, false);

            CurrentBox = Boxes[tries][pos] as Border;
            CurrentTxt = Boxes[tries][pos].Child as TextBlock;

            SelectBox(CurrentBox);
        }

        private void CreateGameGrid(int wordLength)
        {
            Main.Children.Remove(BoxTemplate);
            TextTemplate.Text = "";

            for(int i = 0; i < TRIES; i++)
            {
                Boxes.Add(new List<Border>());

                for (int j = 0; j < wordLength; j++)
                {
                    Border copy = Copy(BoxTemplate) as Border;
                    Thickness t = copy.Margin;
                    t.Left += (BoxTemplate.Width - 1) * j;
                    t.Top += (BoxTemplate.Width - 1) * i;
                    copy.Margin = t;

                    Main.Children.Add(copy);

                    Boxes[i].Add(copy);
                }
            }
        }

        private string TimeSpanToStr(TimeSpan time)
        {
            int h = (int)time.TotalHours;
            int m = time.Minutes;
            int s = time.Seconds;
            return (h > 0 ? string.Format("{0}h", h) : "") + (h > 0 || m > 0 ? string.Format("{0}m", m) : "") + string.Format("{0}s", s);
        }

        private static UIElement Copy(UIElement toCopy)
        {
            return XamlReader.Parse(XamlWriter.Save(toCopy)) as UIElement;
        }

        private List<string> LoadWords()
        {
            string file = File.ReadAllText("sutom.json");
            return JsonConvert.DeserializeObject<List<string>>(file);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void Seed_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Play();
            }
            else if(e.Key == Key.Escape || e.Key == Key.Tab)
            {
                Seed.Text = seed;
                GameText.Focus();
            }
        }

        private void Seed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Seed.Text == seed)
            {
                PlayButton.Content = "Next";
            }
            else
            {
                PlayButton.Content = "Play";
            }
        }
    }
}
