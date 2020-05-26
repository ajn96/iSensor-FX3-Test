using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NUnitLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                PrintHelp();
                return;
            }

            if(args[0] == "help")
            {
                PrintHelp();
                return;
            }

            if(args.Count() < 2)
            {
                Console.WriteLine("ERROR: Invalid number of arguments");
                return;
            }

            string source, dest;
            bool verbose;

            source = args[0];
            dest = args[1];
            verbose = false;
            if(args.Length >= 3)
            {
                if (args[2] == "-verbose")
                {
                    verbose = true;
                    Console.WriteLine("Verbose mode enabled");
                }
                    
            }

            if(!File.Exists(source))
            {
                Console.WriteLine("Invalid source file path: " + source);
                return;
            }

            ProcessLog(source, dest, verbose);
            Console.WriteLine("Program exiting...");
        }

        static void PrintHelp()
        {
            Console.WriteLine("NUnit Log Parser Usage:" + Environment.NewLine);
            Console.WriteLine("NUnitLogParser.exe SourceFile DestFile -verbose" + Environment.NewLine);
            Console.WriteLine("SourceFile is the path to the NUnit test output (XML format) to be parsed");
            Console.WriteLine("Destfile is the path to the image output showing the NUnit XML log summary");
            Console.WriteLine("-verbose is an optional flag to enable verbose mode");
        }

        static void ProcessLog(string SourceXMLPath, string DestImagePath, bool verbose)
        {
            if (verbose)
            {
                Console.WriteLine("Starting log processing");
                Console.WriteLine("Source File: " + SourceXMLPath);
                Console.WriteLine("Destination File: " + DestImagePath);
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(SourceXMLPath);
            }
            catch(Exception e)
            {
                Console.WriteLine("XML Load failed, aborting! " + e.Message);
                return;
            }

            if(verbose)
            {
                Console.WriteLine("XML document loaded...");
            }

            XmlAttributeCollection attributes;
            attributes = doc.LastChild.Attributes;

            if(verbose)
            {
                Console.WriteLine("XLM Attributes:");
                foreach(XmlAttribute at in attributes)
                {
                    Console.WriteLine(at.LocalName + " : " + at.Value);
                }
            }

            if(attributes.Count < 11)
            {
                Console.WriteLine("Invalid XML attributes loaded...");
                return;
            }

            int runs = -1;
            int fails = -1;
            int errors = -1;
            string date = "";
            string time = "";
            try
            {
                foreach (XmlAttribute at in attributes)
                {
                    if(at.LocalName == "time")
                    {
                        time = at.Value;
                    }
                    if (at.LocalName == "date")
                    {
                        date = at.Value;
                    }
                    if (at.LocalName == "errors")
                    {
                        errors = Convert.ToInt32(at.Value);
                    }
                    if (at.LocalName == "total")
                    {
                        runs = Convert.ToInt32(at.Value);
                    }
                    if (at.LocalName == "failures")
                    {
                        fails = Convert.ToInt32(at.Value);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Parsing XLM failed! " + e.Message);
                return;
            }

            if(time == "")
            {
                Console.WriteLine("Test time attribute not found! ");
                return;
            }
            if (date == "")
            {
                Console.WriteLine("Test date attribute not found! ");
                return;
            }
            if (runs == -1)
            {
                Console.WriteLine("Test runs attribute not found! ");
                return;
            }
            if (fails == -1)
            {
                Console.WriteLine("Test fails attribute not found! ");
                return;
            }
            if (errors == -1)
            {
                Console.WriteLine("Test errors attribute not found! ");
                return;
            }

            /* Count errors as test failures */
            fails += errors;

            if (verbose)
                Console.WriteLine("Document parsed, starting image generation...");

            WriteImage(DestImagePath, runs, fails, date, time);
        }

        static void WriteImage(string ImagePath, int testsRun, int testFails, string date, string time)
        {
            /* Build string to put in result image */
            string result = "NUnit Test Results: " + Environment.NewLine + Environment.NewLine + 
                "Tests Run: " + testsRun.ToString() + Environment.NewLine +
                "Tests Failing: " + testFails.ToString() + Environment.NewLine +
                "Test Date: " + date + Environment.NewLine +
                "Test Time: " + time;

            /* Create new drawing object */
            Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);

            Font imgFont = new Font("microsoft sans serif", 14.0f, FontStyle.Regular, GraphicsUnit.Pixel);

            /* Get image size required to store text */
            SizeF textSize = drawing.MeasureString(result, imgFont);

            /* Dispose of old images */
            img.Dispose();
            drawing.Dispose();

            /* create a new image */
            img = new Bitmap((int)textSize.Width + 4, (int)textSize.Height + 4);

            drawing = Graphics.FromImage(img);

            /* paint the background (color depends on if tests pass/fail) */
            if(testFails == 0)
            {
                if(testsRun == 0)
                {
                    drawing.Clear(Color.LightYellow);
                }
                else
                {
                    drawing.Clear(Color.LightGreen);
                }
            }
            else
            {
                drawing.Clear(Color.Red);
            }
            
            /* create a brush for the text */
            Brush textBrush = new SolidBrush(Color.Black);

            /* Draw text */
            drawing.DrawString(result, imgFont, textBrush, 2, 2);

            /* Draw border */
            Pen pen = new Pen(Color.Black, 2);
            pen.Alignment = PenAlignment.Inset;
            drawing.DrawRectangle(pen, 0, 0, img.Width, img.Height);

            /* Save image */
            drawing.Save();

            /* Free resources */
            textBrush.Dispose();
            drawing.Dispose();

            /* Save image */
            try
            {
                img.Save(ImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch(Exception e)
            {
                Console.WriteLine("Image save to " + ImagePath + " failed! " + e.Message);
                return;
            }
            img.Dispose();
        }
    }
}
