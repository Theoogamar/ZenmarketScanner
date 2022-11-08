using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZenmarketScanner.Models;
using Newtonsoft.Json;
using System.Web;

namespace ZenmarketScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // folder path for all the saved searches
        private const string JsonPath = "./Searches/";

        // file path for the saved index for scanning that item on launch
        private const string LaunchPath = "./DefaultItem.json";

        // the object that scrapes Zenmarket
        private readonly ScrapeZenmarket ScrapeZenmarket = new ScrapeZenmarket();

        // what item is set on startup
        private string DefaultItem;

        // lists
        private List<ZenmarketItem2> itemsAdded;
        private List<ZenmarketItem2> itemsSold;

        // indexs to keep track of the current item the user is at in the lists
        private int AddedIdx;
        private int SoldIdx;

        public MainWindow()
        {
            InitializeComponent();

            // initalize events
            ScrapeZenmarket.LoadProgressChanged += ScrapeZenmarket_LoadProgressChanged;
            ScrapeZenmarket.LoadComplete += ScrapeZenmarket_LoadComplete;

            // creates the directory
            Directory.CreateDirectory(JsonPath);

            // loads of the save searches in the application
            string[] filePaths = Directory.GetFiles(JsonPath, "*.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < filePaths.Length; i++)
            {
                // load non-asynchronously
                string json = File.ReadAllText(filePaths[i]);
                ZenmarketSearch search = JsonConvert.DeserializeObject<ZenmarketSearch>(json);

                //add to the combo box
                SavedCombox.Items.Add(search);
            }

            // checking the "is default" check
            if (File.Exists(LaunchPath))
            {
                // load item from disk and check if it exists
                DefaultItem = File.ReadAllText(LaunchPath);

                // if no check return
                if (DefaultItem == "")
                    return;

                // search for the item that need to be set in the combobox
                foreach (ZenmarketSearch item in SavedCombox.Items)
                {
                    if (item.Name == DefaultItem)
                    {
                        // set it
                        SavedCombox.SelectedItem = item;
                        IsDefaultChkbox.IsChecked = true;
                    }
                }
                
            }
            else
                File.Create(LaunchPath);
        }

        // scan progress event
        private void ScrapeZenmarket_LoadProgressChanged(object sender, LoadProgressChangedEventArgs e)
        {
            Progbar.Value = e.Percentage;
            ProgTxt.Text = $"{e.Percentage}%{e.Info}";
        }

        // scan finished event
        private void ScrapeZenmarket_LoadComplete(object sender, EventArgs e)
        {
            Progbar.Value = 0;
            ProgTxt.Text = "";
        }

        // searchs Zenmarket and saves the results
        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            // check if the item is already added
            foreach (ZenmarketSearch item in SavedCombox.Items)
                if (item.Name == NameTxtbox.Text)
                    return;

            SearchBtn.IsEnabled = false;

            // initize search object
            ZenmarketSearch search = new ZenmarketSearch
            {
                Name = NameTxtbox.Text,
                Search = SearchTxtbox.Text,
                Date = DateTime.Now
            };

            // reset the text boxes
            NameTxtbox.Text = "";
            SearchTxtbox.Text = "";

            // search Zenmarket for all the listings in the search query
            var tuple = await ScrapeZenmarket.APIZenMarket(search.Search);

            // searched data
            search.Data = tuple.Item1;

            // tell the user how many pages where searched
            PagesSearchedTxt.Text = tuple.Item2.ToString();

            // save the data to disk
            string json = JsonConvert.SerializeObject(search, Formatting.Indented);
            await File.WriteAllTextAsync($"{JsonPath}{search.Name}.json", json);

            // add it to the combo box
            SavedCombox.Items.Add(search);
        }

        // checks if the search button can be enabled
        private void NameTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NameTxtbox.Text != "" && SearchTxtbox.Text != "")
                SearchBtn.IsEnabled = true;
            else
                SearchBtn.IsEnabled = false;
        }

        // checks if the search button can be enabled
        private void SearchTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NameTxtbox.Text != "" && SearchTxtbox.Text != "")
                SearchBtn.IsEnabled = true;
            else
                SearchBtn.IsEnabled = false;
        }

        // change the information based on what saved search the user selects
        private void SavedCombox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // if selected nothing return
            if (SavedCombox.SelectedIndex == -1)
                return;

            // get the saved search
            ZenmarketSearch search = (ZenmarketSearch)SavedCombox.SelectedItem;

            // the search query
            SearchTermTxt.Text = search.Search;
            
            // date when last searched
            DateTxt.Text = $"{search.Date:d}"; // {search.Date:t}

            // enable the can button
            ScanBtn.IsEnabled = true;

            // enable the check box
            IsDefaultChkbox.IsEnabled = true;

            // clear pages search
            PagesSearchedTxt.Text = "";

            // enable the selet button
            DeleteBtn.IsEnabled = true;

            // if the item has been select to be scanned on launch
            if (DefaultItem == ((ZenmarketSearch)SavedCombox.SelectedItem).Name)
                IsDefaultChkbox.IsChecked = true;
            else
                IsDefaultChkbox.IsChecked = false;
        }

        // logic for scanning a search
        private async void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            ScanBtn.IsEnabled = false;

            // reset some values
            itemsAdded = new List<ZenmarketItem2>();
            itemsSold = new List<ZenmarketItem2>();
            AddedIdx = 0;
            SoldIdx = 0;
            AddedLeftBtn.IsEnabled = false;
            AddedRightBtn.IsEnabled = false;
            SoldLeftBtn.IsEnabled = false;
            SoldRightBtn.IsEnabled = false;

            // get the item to be scanned
            ZenmarketSearch search = (ZenmarketSearch)SavedCombox.SelectedItem;

            // search all the items on Zenmarket and get a new dict
            (Dictionary<string, ZenmarketItem> newData, int pages) = await ScrapeZenmarket.APIZenMarket(search.Search);

            // compare the old and the dict get lists of item that are add and remove
            (itemsAdded, itemsSold) = await ScrapeZenmarket.CheckListingIsSold(search.Data, newData);

            // how many pages where searched
            PagesSearchedTxt.Text = pages.ToString();

            // enable items added UI
            if (itemsAdded.Count != 0)
            {
                if (itemsAdded.Count > 1)
                    AddedRightBtn.IsEnabled = true;

                AddedNumTxt.Text = $"{AddedIdx + 1} / {itemsAdded.Count}";
                AddedPrice.Text = $"{itemsAdded[AddedIdx].Price}, Bids: {itemsAdded[AddedIdx].Bids}";
                LoadImage(AddedImg, itemsAdded[AddedIdx].ImgUrl);
            }

            // enable items gone UI
            if (itemsSold.Count != 0)
            {
                if (itemsSold.Count > 1)
                    SoldRightBtn.IsEnabled = true;

                SoldNumTxt.Text = $"{SoldIdx + 1} / {itemsSold.Count}";
                SoldPrice.Text =  $"{itemsSold[SoldIdx].Price}, Bids: {itemsSold[AddedIdx].Bids}";
                LoadImage(SoldImg, itemsSold[SoldIdx].ImgUrl);
            }

            // update date scanned
            search.Data = newData;
            search.Date = DateTime.Now;

            // display new date
            DateTxt.Text = $"{search.Date:d}"; // {search.Date:t} 

            // save new saved search
#if !DEBUG
            string json = JsonConvert.SerializeObject(search, Formatting.Indented);
            await File.WriteAllTextAsync($"{JsonPath}{search.Name}.json", json);
#endif
            ScanBtn.IsEnabled = true;
        }

        // is default check box click logic
        private void IsDefaultChkbox_Click(object sender, RoutedEventArgs e)
        {
            // check if checked or not
            if ((bool)IsDefaultChkbox.IsChecked)
            {
                // if checked save new saved search name to disk
                DefaultItem = ((ZenmarketSearch)SavedCombox.SelectedItem).Name;
                File.WriteAllText(LaunchPath, DefaultItem);
            }
            else
            {
                // if checked saved empty string to disk
                DefaultItem = "";
                File.WriteAllText(LaunchPath, DefaultItem);
            }
        }

        // logic for deleting a saved search
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            // get the saved search that is to be deleted
            ZenmarketSearch search = (ZenmarketSearch)SavedCombox.SelectedItem;

            // delete the item from disk
            File.Delete($"{JsonPath}{search.Name}.json");

            // if it was ticked to be scanned on launch clear that information
            if (DefaultItem == ((ZenmarketSearch)SavedCombox.SelectedItem).Name)
            {
                DefaultItem = "";
                string json = JsonConvert.SerializeObject(-1);
                File.WriteAllText(LaunchPath, json);
            }

            // removes all the infomation for the UI
            SavedCombox.Items.Remove(search);

            SearchTermTxt.Text = "";

            DateTxt.Text = "";

            ScanBtn.IsEnabled = false;

            PagesSearchedTxt.Text = "";

            IsDefaultChkbox.IsEnabled = false;
            IsDefaultChkbox.IsChecked = false;

            DeleteBtn.IsEnabled = false;
        }

        // logic for going left through the items added
        private void AddedLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            // update index
            AddedIdx--;

            // checks if at very left
            if (AddedIdx == 0)
                AddedLeftBtn.IsEnabled = false;

            // enables right button when its 1 from the very right
            if (AddedIdx < itemsAdded.Count - 1)
                AddedRightBtn.IsEnabled = true;

            // update text
            AddedNumTxt.Text = $"{AddedIdx + 1} / {itemsAdded.Count}";
            AddedPrice.Text = $"{itemsAdded[AddedIdx].Price}, Bids: {itemsAdded[AddedIdx].Bids}";

            // loads image
            LoadImage(AddedImg, itemsAdded[AddedIdx].ImgUrl);
        }

        // logic for going right through the items added
        private void AddedRightBtn_Click(object sender, RoutedEventArgs e)
        {
            // update index
            AddedIdx++;

            // checks if at very right
            if (AddedIdx != 0)
                AddedLeftBtn.IsEnabled = true;

            // enables left button when its 1 from the very left
            if (AddedIdx == itemsAdded.Count - 1)
                AddedRightBtn.IsEnabled = false;

            // update text
            AddedNumTxt.Text = $"{AddedIdx + 1} / {itemsAdded.Count}";
            AddedPrice.Text = $"{itemsAdded[AddedIdx].Price}, Bids: {itemsAdded[AddedIdx].Bids}";

            // loads image
            LoadImage(AddedImg, itemsAdded[AddedIdx].ImgUrl);
        }

        // logic for going left through the items gone
        private void SoldLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            // update index
            SoldIdx--;

            // checks if at very left
            if (SoldIdx == 0)
                SoldLeftBtn.IsEnabled = false;

            // enables right button when its 1 from the very right
            if (SoldIdx < itemsSold.Count - 1)
                SoldRightBtn.IsEnabled = true;

            // update text
            SoldNumTxt.Text = $"{SoldIdx + 1} / {itemsSold.Count}";
            SoldPrice.Text = $"{itemsSold[SoldIdx].Price}, Bids: {itemsSold[SoldIdx].Bids}";

            // loads image
            LoadImage(SoldImg, itemsSold[SoldIdx].ImgUrl);
        }

        // logic for going right through the items gone
        private void SoldRightBtn_Click(object sender, RoutedEventArgs e)
        {
            // update index
            SoldIdx++;

            // checks if at very right
            if (SoldIdx != 0)
                SoldLeftBtn.IsEnabled = true;

            // enables left button when its 1 from the very left
            if (SoldIdx == itemsSold.Count - 1)
                SoldRightBtn.IsEnabled = false;

            // update text
            SoldNumTxt.Text = $"{SoldIdx + 1} / {itemsSold.Count}";
            SoldPrice.Text = $"{itemsSold[SoldIdx].Price}, Bids: {itemsSold[SoldIdx].Bids}";

            // loads image
            LoadImage(SoldImg, itemsSold[SoldIdx].ImgUrl);
        }

        // loads an image in the image box
        private void LoadImage(Image img, string url)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.EndInit();
            
            img.Source = bitmap;
        }

        // launchs the item in the browser when clicked on the image box
        private void AddedImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ScrapeZenmarket.GetItemUrl(itemsAdded[AddedIdx].Id),
                UseShellExecute = true
            });
        }
        private void SoldImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ScrapeZenmarket.GetItemUrl(itemsSold[SoldIdx].Id),
                UseShellExecute = true
            });
        }

        // launch search term
        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ScrapeZenmarket.GetSearchUrl(SearchTermTxt.Text),
                UseShellExecute = true
            });
        }
    }
}
