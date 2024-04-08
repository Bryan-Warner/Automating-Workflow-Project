using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using SeleniumExtras.WaitHelpers;
using System.Text.RegularExpressions;


namespace Music
{
    class Automation
    {
        private static bool errorpause = false;
        //Method that allows the user to pause the program make changes and then resume the program
        private static void WaitForDotInputOrTimeout()
        {
            Console.WriteLine("Press '.' to pause. Press '/' to continue after pausing.");

            var pauseTask = Task.Run(() =>
            {
                // Check if the '.' is pressed
                if (Console.KeyAvailable && Console.ReadKey(true).KeyChar == '.' || errorpause)
                {
                    Console.WriteLine("Paused. Press '/' to continue...");
                    errorpause = false;
                    // Loop until '/' is pressed
                    while (true)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).KeyChar == '/')
                        {
                            break; // Exit the loop once '/' is pressed
                        }
                        Thread.Sleep(100); // Add a short delay to reduce CPU usage
                    }
                }
                Thread.Sleep(100);
            });

            // Wait indefinitely for the pauseTask to complete, which only happens after '/' is pressed.
            pauseTask.Wait();
        }

        private static bool IsAlertPresent(IWebDriver driver)
        {
            try
            {
                driver.SwitchTo().Alert();
                return true; // Alert found
            }
            catch (NoAlertPresentException)
            {
                return false; // No alert found
            }
        }

        //Method for tier 2 distributers. This method is used to clean the data and update it in the database.
        private static void Tier2()
        {

            new DriverManager().SetUpDriver(new ChromeConfig());

            // Initialize the ChromeDriver (this will open a Chrome window)
            using (IWebDriver driver = new ChromeDriver())
            {

                // Navigate to a website
                driver.Navigate().GoToUrl("https://www.google.com/");
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Refresh the page
                driver.Navigate().Refresh();

                // Maximize the window
                driver.Manage().Window.Maximize();
                Thread.Sleep(45000); // Allow time to access TSA webapp and enter the search criteria
                int rowIndex = 0; // Start with the first row
                bool hasMoreRows = true;
                // Loop through each music record in the search results
                while (hasMoreRows)
                {

                    driver.SwitchTo().Frame("search");

                    // Attempt to find the row at the current index
                    IList<IWebElement> currentTableRows = driver.FindElements(By.CssSelector("#dgResults tbody tr"));

                    if (rowIndex < currentTableRows.Count)
                    {
                        // Wait until the row is clickable and then click it
                        //Tier 2 method verifies the data which will remove it from the seach results, so we will always click the first row.
                        var rowToClick = wait.Until(ExpectedConditions.ElementToBeClickable(currentTableRows[0]));
                        rowToClick.Click();



                        // Perform actions after clicking the row
                        //cleaning data
                        driver.SwitchTo().DefaultContent();

                        // Switch to the 'proddetails' frame
                        driver.SwitchTo().Frame("proddetails");

                        var songTitleElement = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("tbTitle")));


                        string songTitle = songTitleElement.GetAttribute("value");

                        string pattern = @"\((?:(\d+)?(?:cd|lp|DVD|cass|ep|ost|\d+|cd\+DVD)\s*)+\)";
                        // Regular expression to match unwanted additional information. additional information is enclosed in parentheses
                        RegexOptions options = RegexOptions.IgnoreCase; // Use the IgnoreCase option to make the pattern case-insensitive

                        // Replace the matched content with an empty string to "remove" it
                        string cleanedTitle = Regex.Replace(songTitle, pattern, "", options).Trim();
                        //Javascript code to update the title
                        string jsCode = $"document.getElementById('tbTitle').value = '{cleanedTitle.Replace("'", "\\'")}';";
                        // Execute the JavaScript code
                        ((IJavaScriptExecutor)driver).ExecuteScript(jsCode);


                        // Check if there is still () in the title, this information is required.Add it to the free text.

                        string songTitle1 = songTitleElement.GetAttribute("value");
                        Match match = Regex.Match(songTitle1, @"\(([^)]*)\)");

                        // Check if we found a match (content within parentheses)
                        if (match.Success)
                        {
                            // Extract the content inside the parentheses
                            string contentInsideParentheses = match.Groups[1].Value;

                            // Remove the parentheses and the content inside from the song title
                            string newSongTitle1 = Regex.Replace(songTitle1, @"\s*\([^)]*\)", "").Trim();

                            // Set the modified song title back to the element
                            songTitleElement.Clear();
                            songTitleElement.SendKeys(newSongTitle1);

                            // Find the free text input element
                            var freeTextElement = driver.FindElement(By.Id("tbFreeText"));

                            // Add the content that was inside the parentheses to the free text input's value
                            freeTextElement.SendKeys(contentInsideParentheses);
                        }

                        //Clicking the save button

                        var saveButton = driver.FindElement(By.Id("btnSave"));
                        saveButton.Click();
                        //Check if there is an alert, if there is pause the program. User can make adjustments if needed, and then resume the program.
                        if (IsAlertPresent(driver))
                        {
                            errorpause = true;
                            WaitForDotInputOrTimeout();
                        }
                        // verify the record
                        errorpause = false;
                        var verifyButton = driver.FindElement(By.Id("btnVerified"));
                        verifyButton.Click();


                        Thread.Sleep(3000);
                        WaitForDotInputOrTimeout();

                        rowIndex++;
                        driver.SwitchTo().DefaultContent();
                    }
                    else
                    {
                        // No more rows to process
                        hasMoreRows = false;
                    }


                }

                // Close the browser window
                driver.Quit();
            }
        }



        static void Main(string[] args)
        {
            // Uncomment method based on the tier of distributer being worked on
            //Tier1();
            Tier2();
        }

    }
}
