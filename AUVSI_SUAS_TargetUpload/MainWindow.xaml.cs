
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Globalization;

namespace AUVSI_SUAS_TargetUpload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*
         * This program allows for the upload of ODLC images to the judges server at the AUVSI competition. 
         * The API Specification can be found here: https://github.com/auvsi-suas/interop#api-specification
         * All global variables prefixed with a g
         */

        private readonly HttpClient gHttpClient;
        private bool gLoggedIn;

        //Stores all the ODLC data
        private ObservableCollection<ODLC> odlcList;

        public MainWindow()
        {
            InitializeComponent();

            //Initialize the HttpClient object 
            gHttpClient = new HttpClient();
            //Set timeout to 30 minutes (overkill, but whatever)
            gHttpClient.Timeout = new TimeSpan(0, 30, 0);
            gLoggedIn = false;

            //Set status label to empty string.
            StatusLabel.Content = "";

            //Initialize new odlcList object 
            odlcList = new ObservableCollection<ODLC>();


            //Set datasource for the listbox
            Listbox_ODLC.ItemsSource = odlcList;

            //Set itemsource for the comboboxes in the UI
            Combobox_Type.ItemsSource = Enum.GetValues(typeof(ODLC.ODLCType));
            Combobox_Orientation.ItemsSource = Enum.GetValues(typeof(ODLC.ODLCOrientation));
            Combobox_Shape.ItemsSource = Enum.GetValues(typeof(ODLC.ODLCShape));
            Combobox_ShapeColour.ItemsSource = Enum.GetValues(typeof(ODLC.ODLCColor));
            Combobox_AlphanumericColour.ItemsSource = Enum.GetValues(typeof(ODLC.ODLCColor));

            //Login();
            //List<ODLC> temp = getOLDC();
            //ODLC temp2 = getOLDC(1);
        }

        /// <summary>
        /// Function to log into the interop server using the supplied username and password.
        /// The HttpClient stores the cookie given by the server to use for future communication.
        /// </summary>
        private async Task<bool> Login()
        {
            //Set base address of the httpclient in case it was changed. 
            //gHttpClient.BaseAddress = new Uri(Properties.Settings.Default.url); //We can't set the base address again once we've tried sending a request, and we can't make a new HttpClient object.  
            string login = string.Format("{{ \"username\":\"{0}\",\"password\":\"{1}\"}}", Properties.Settings.Default.username, Properties.Settings.Default.password);
            StringContent content = new StringContent(login, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await gHttpClient.PostAsync(Properties.Settings.Default.url + "/api/login", content);
                //HttpResponseMessage response = foo(content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    gLoggedIn = true;
                    return true;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //Username is incorrect
                    MessageBox.Show("The provided username and password is incorrect. Please verify and try again", "Invalid Username or Password", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                Exception exception = ex;
                string message = "An error occured when attempting to connect to the server.\r\nPlease verify the correct URL is specified.\r\n\r\nException Messages:\r\n";
                while (true)
                {
                    message += exception.Message + "\r\n";
                    if (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }
                    else
                    {
                        break;
                    }
                }
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Error");
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Error");
            }


            return false;
        }

        /// <summary>
        /// Uploads a new ODLC object to the server. Returns the uploaded object. 
        /// </summary>
        /// <param name="odlcObject"></param>
        /// <returns></returns>
        private async Task<ODLC> postODLC(ODLC odlcObject)
        {
            if (!gLoggedIn)
            {
                return null;
            }
            //If the ID is set, then we have already uploaded this object 
            if (odlcObject.ID != null)
            {
                return null;
            }
            ODLC odlcResponse = null;
            try
            {
                StringContent content = new StringContent(odlcObject.getJson(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await gHttpClient.PostAsync(Properties.Settings.Default.url + "/api/odlcs", content);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    odlcResponse = JsonConvert.DeserializeObject<ODLC>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    //Response code was not "ok"
                    MessageBox.Show("Error: " + response.StatusCode.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception Ex)
            {
                //Log something
                return null;
            }

            return odlcResponse;
        }


        /// <summary>
        /// Updates the uploaded ODLC object on the server. Returns the updated object. 
        /// </summary>
        /// <param name="odlcObject"></param>
        /// <returns></returns>
        private async Task<ODLC> updateODLC(ODLC odlcObject)
        {
            if (!gLoggedIn)
            {
                return null;
            }
            //If the ID is not set, then the object has not been uploaded yet. 
            if (odlcObject.ID == null)
            {
                return null;
            }
            ODLC odlcResponse = null;
            string id = odlcObject.ID.ToString();
            try
            {
                StringContent content = new StringContent(odlcObject.getJson(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await gHttpClient.PutAsync(Properties.Settings.Default.url + "/api/odlcs/" + id, content);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string temp = await response.Content.ReadAsStringAsync();
                    odlcResponse = JsonConvert.DeserializeObject<ODLC>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    //Response code was not "ok"
                    MessageBox.Show("Error: " + response.StatusCode.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception Ex)
            {
                //Log something
                return null;
            }

            return odlcResponse;
        }

        //Delete Function 
        //Not mentioned in the interop program specs, but it does work. 
        //gHttpClient.DeleteAsync("/api/odlcs" + id.ToString())

        private async Task<bool> deleteODLC(int id)
        {
            if (!gLoggedIn)
            {
                return false;
            }
            try
            {
                HttpResponseMessage response = await gHttpClient.DeleteAsync(Properties.Settings.Default.url + "/api/odlcs/" + id.ToString());
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                    //odlcList = JsonConvert.DeserializeObject<List<ODLC>>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    //Response code was not "ok"
                    MessageBox.Show("Error: " + response.StatusCode.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception Ex)
            {
                //Log something
                return false;
            }
        }

        /// <summary>
        /// Gets a list of ODLC targets uploaded to the server. Returns NULL from any server errors. 
        /// </summary>
        /// <returns></returns>
        private async Task<List<ODLC>> getOLDC()
        {
            if (!gLoggedIn)
            {
                return null;
            }
            List<ODLC> odlcList = null;
            try
            {
                HttpResponseMessage response = await gHttpClient.GetAsync(Properties.Settings.Default.url + "/api/odlcs");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    odlcList = JsonConvert.DeserializeObject<List<ODLC>>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    //Response code was not "ok"
                    MessageBox.Show("Error: " + response.StatusCode.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception Ex)
            {
                //Log something
                return null;
            }

            return odlcList;
        }

        /// <summary>
        /// Gets a single ODLC object uploaded from the server. Returns NULL if id is invalid. 
        /// </summary>
        /// <returns>
        /// A single ODLC object with the given id. Returns NULL if id is invalid. 
        /// </returns>
        private ODLC getOLDC(int id)
        {
            if (!gLoggedIn)
            {
                return null;
            }
            ODLC odlcObject = null;
            try
            {
                HttpResponseMessage response = gHttpClient.GetAsync(Properties.Settings.Default.url + "/api/odlcs/" + id.ToString()).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    odlcObject = JsonConvert.DeserializeObject<ODLC>(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    //Response code was not "ok"
                    MessageBox.Show("Error: " + response.StatusCode.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception Ex)
            {
                //Log something
                return null;
            }

            return odlcObject;
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                //We create new bitmaps for each, because we don't want to thumbnail to be modified when we change the target image. 
                ((ODLC)Listbox_ODLC.SelectedItem).TargetImage = new Bitmap(openFileDialog.FileName);
                ((ODLC)Listbox_ODLC.SelectedItem).ThumbnailImage = new Bitmap(openFileDialog.FileName);
            }
        }

        private void AddTarget_Click(object sender, RoutedEventArgs e)
        {
            odlcList.Add(new ODLC());
            if (odlcList.Count == 1)
            {
                Listbox_ODLC.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Deletes the selected object in the listview. If the target has been uploaded to the server, we will mark it to delete.
        /// If the target is only local, we ask before deleting it forever. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteTarget_Click(object sender, RoutedEventArgs e)
        {

            ODLC objectToDelete = (ODLC)Listbox_ODLC.SelectedItem;

            //Object is local, we don't have to make changes on the server. 
            if (objectToDelete.SyncStatus == ODLC.ODLCSyncStatus.UNSYNCED)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this target?\r\nThis cannot be undone.", "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        odlcList.Remove((ODLC)Listbox_ODLC.SelectedItem);
                    }
                    catch
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            //Object is on the server. We instruct the server to delete the object next time we sync. 
            else
            {
                objectToDelete.SyncStatus = ODLC.ODLCSyncStatus.DELETE;
            }


        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.BeginInvoke(new Action(delegate
            {
                StatusLabel.Content = "Connecting to Server...";
            }));

            bool result = await Login();

            if (result)
            {
                await Dispatcher.BeginInvoke(new Action(delegate
                {
                    StatusLabel.Content = "Connected";
                }));

            }
            else
            {
                await Dispatcher.BeginInvoke(new Action(delegate
                {
                    StatusLabel.Content = "Connection Failed";
                }));
            }

            //Sync with the server, and get all uploaded targets, in case we accidentally closed the program, or are working on two different computers. 
            List<ODLC> uploadedODLCs = await getOLDC();
            syncODLC(uploadedODLCs);

        }

        /// <summary>
        /// Syncs the list of ODLCs on the server with our local copy. 
        /// </summary>
        /// <param name="serverODLCs"></param>
        private void syncODLC(List<ODLC> serverODLCs)
        {
            foreach (ODLC i in serverODLCs)
            {
                bool targetExistsLocally = false;
                //If target ID on the server matches the one on our local list, we consider the local copy the good one, and we don't do anything. 
                foreach (ODLC j in odlcList)
                {
                    if (j.ID == i.ID)
                    {
                        targetExistsLocally = true;                  
                    }
                }
                if (!targetExistsLocally)
                {
                    i.SyncStatus = ODLC.ODLCSyncStatus.SYNCED;
                    odlcList.Add(i);
                }
            }
        }

        /// <summary>
        /// Uploads all changes to the server and updates the UI with the response, such as ID or other things. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Sync_Click(object sender, RoutedEventArgs e)
        {
            if (!gLoggedIn)
            {
                await Dispatcher.BeginInvoke(new Action(delegate
                {
                    StatusLabel.Content = "Please connect to server first!";
                }));
                return;
            }

            int numTargets = odlcList.Count();

            //Sync local copy with server. 
            List<ODLC> temp = await getOLDC();
            syncODLC(temp);

            ODLC returnedObject;
            for (int i = 0; i < numTargets; i++)
            {
                
                await Dispatcher.BeginInvoke(new Action(delegate
                {
                    StatusLabel.Content = String.Format("Syncing Target {0} of {1}", i, numTargets);
                }));

                if (odlcList[i].SyncStatus == ODLC.ODLCSyncStatus.DELETE)
                {
                    //Delete the object
                    bool deleted = await deleteODLC((int)odlcList[i].ID);
                    if (deleted)
                    {
                        odlcList.RemoveAt(i);
                        numTargets--;
                        i--;
                    }
                    continue;
                }

                if (odlcList[i].ID == null)
                {
                    returnedObject = await postODLC(odlcList[i]);
                    if(returnedObject!= null)
                    {
                        odlcList[i].CopyObjectSettingsFromServer(returnedObject);
                    }
                }
                else
                {
                    returnedObject = await updateODLC(odlcList[i]);
                    if (returnedObject != null)
                    {
                        odlcList[i].CopyObjectSettingsFromServer(returnedObject);
                    }
                }
            }

            if (odlcList.Count > 0)
            {
                Listbox_ODLC.SelectedIndex = 0;
            }

            await Dispatcher.BeginInvoke(new Action(delegate
            {
                StatusLabel.Content = "Sync Complete";
            }));
        }
    }

    /// <summary>
    /// Class that holds the OLDC object. 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ODLC : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if(syncStatus != ODLCSyncStatus.UNSYNCED && syncStatus != ODLCSyncStatus.DELETE && propertyName != "SyncStatus")
            {
                SyncStatus = ODLCSyncStatus.MODIFIED;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //We cannot send an id when uploading a new object. We only set ID when updating the object. 
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        private int? id;

        public int? ID
        {
            get
            {
                return id;
            }
            set
            {
                if (value != id)
                {
                    id = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("mission")]
        private int mission;

        /// <summary>
        /// The mission that the OLDC belongs to. 
        /// </summary>
        public int Mission
        {
            get
            {
                return mission;
            }
            set
            {
                if (value != mission)
                {
                    mission = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        private ODLCType type;

        public ODLCType Type
        {
            get
            {
                return type;
            }
            set
            {
                if (value != type)
                {
                    type = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("latitude")]
        private double latitude;

        public double Latitude
        {
            get
            {
                return latitude;
            }
            set
            {
                if (value != latitude)
                {
                    latitude = value;
                    NotifyPropertyChanged();
                }
            }
        }



        [JsonProperty("longitude")]
        private double longitude;

        public double Longitude
        {
            get
            {
                return longitude;
            }
            set
            {
                if (value != longitude)
                {
                    longitude = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("orientation")]
        [JsonConverter(typeof(StringEnumConverter))]
        private ODLCOrientation orientation;

        public ODLCOrientation Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                if (value != orientation)
                {
                    orientation = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("shape")]
        [JsonConverter(typeof(StringEnumConverter))]
        private ODLCShape shape;

        public ODLCShape Shape
        {
            get
            {
                return shape;
            }
            set
            {
                if (value != shape)
                {
                    shape = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("shapeColor")]
        [JsonConverter(typeof(StringEnumConverter))]
        private ODLCColor shapeColor;

        public ODLCColor ShapeColor
        {
            get
            {
                return shapeColor;
            }
            set
            {
                if (value != shapeColor)
                {
                    shapeColor = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("alphanumeric")]
        private string alphanumeric;

        public string Alphanumeric
        {
            get
            {
                return alphanumeric;
            }
            set
            {
                if (value != alphanumeric)
                {
                    alphanumeric = value;
                    NotifyPropertyChanged();
                }
            }
        }


        [JsonProperty("alphanumericColor")]
        [JsonConverter(typeof(StringEnumConverter))]
        private ODLCColor alphanumericColor;

        public ODLCColor AlphanumericColor
        {
            get
            {
                return alphanumericColor;
            }
            set
            {
                if (value != alphanumericColor)
                {
                    alphanumericColor = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("description")]
        private string description;

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                if (value != description)
                {
                    description = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonProperty("autonomous")]
        private bool autonomous;

        public bool Autonomous
        {
            get
            {
                return autonomous;
            }
            set
            {
                if (value != autonomous)
                {
                    autonomous = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private ODLCSyncStatus syncStatus;

        public ODLCSyncStatus SyncStatus
        {
            get
            {
                return syncStatus;
            }
            set
            {
                if (value != syncStatus)
                {
                    syncStatus = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Bitmap targetImage;

        public Bitmap TargetImage
        {
            get
            {
                return targetImage;
            }
            set
            {
                if (value != targetImage)
                {
                    targetImage = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("TargetImage_BitmapImage");
                }
            }
        }

        private Bitmap thumbnailImage;

        public Bitmap ThumbnailImage
        {
            get
            {
                return thumbnailImage;
            }
            set
            {
                if (value != thumbnailImage)
                {
                    thumbnailImage = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ThumbnailImage_BitmapImage");
                }
            }
        }


        //Returns a BitmapImage to be displayed on the listbox as a thumbnail. 
        public BitmapImage ThumbnailImage_BitmapImage
        {
            get
            {
                //Databelow copied from  https://stackoverflow.com/questions/22499407/how-to-display-a-bitmap-in-a-wpf-image
                using (MemoryStream memory = new MemoryStream())
                {
                    thumbnailImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();

                    return bitmapimage;
                }
            }
        }

        //Returns a BitmapImage object to be displayed for the image cropper
        public BitmapImage TargetImage_BitmapImage
        {
            get
            {
                //Code below copied from  https://stackoverflow.com/questions/22499407/how-to-display-a-bitmap-in-a-wpf-image
                using (MemoryStream memory = new MemoryStream())
                {
                    TargetImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();

                    return bitmapimage;
                }
            }
        }

        public ODLC()
        {
            id = null;
            mission = 1;
            type = ODLCType.STANDARD;
            latitude = 0;
            longitude = 0;
            orientation = ODLCOrientation.N;
            shape = ODLCShape.CIRCLE;
            shapeColor = ODLCColor.BLUE;
            alphanumeric = "A";
            alphanumericColor = ODLCColor.BLUE;
            description = "";
            autonomous = false;
            syncStatus = ODLCSyncStatus.UNSYNCED;
            thumbnailImage = new Bitmap(Properties.Resources.PlaceholderImage);
            targetImage = new Bitmap(Properties.Resources.PlaceholderImage);
        }

        /// <summary>
        /// Used to copy the object returned from the server, since it has been validated. 
        /// </summary>
        /// <param name="odlcObject"></param>
        public void CopyObjectSettingsFromServer(ODLC odlcObject)
        {
            ID = odlcObject.id;
            Mission = odlcObject.mission;
            Type = odlcObject.type;
            Latitude = odlcObject.latitude;
            Longitude = odlcObject.longitude;
            Orientation = odlcObject.orientation;
            Shape = odlcObject.shape;
            ShapeColor = odlcObject.shapeColor;
            Alphanumeric = odlcObject.alphanumeric;
            AlphanumericColor = odlcObject.alphanumericColor;
            Description = odlcObject.description;
            Autonomous = false;
            SyncStatus = ODLCSyncStatus.SYNCED;
        }

        public string getJson()
        {
            return JsonConvert.SerializeObject(this, new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        public enum ODLCType
        {
            // Standard ODLCs take latitude, longitude, orientation, shape and
            // color, alphanumeric and color, and if processed autonomously.
            // Includes Off Axis ODLC
            STANDARD,
            // Emergent takes latitude, longitude, description, and if process
            // autonomously.
            EMERGENT
        }

        public enum ODLCShape
        {
            CIRCLE,
            SEMICIRCLE,
            QUARTER_CIRCLE,
            TRIANGLE,
            SQUARE,
            RECTANGLE,
            TRAPEZOID,
            PENTAGON,
            HEXAGON,
            HEPTAGON,
            OCTAGON,
            STAR,
            CROSS
        }

        public enum ODLCColor
        {
            WHITE,
            BLACK,
            GRAY,
            RED,
            BLUE,
            GREEN,
            YELLOW,
            PURPLE,
            BROWN,
            ORANGE
        }

        public enum ODLCOrientation
        {
            N,
            NE,
            E,
            SE,
            S,
            SW,
            W,
            NW
        }

        public enum ODLCSyncStatus
        {
            DELETE,     //Object marked for deletion on the server 
            SYNCED,     //Object synced to the server
            UNSYNCED,   //Object not synced on the server 
            MODIFIED    //Object synced, and has since been modified. 
        }
    }

    public class EnumToStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return Enum.GetName(value.GetType(), value);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    //Visibility converter for Standard Target Fields
    public class StandardTargetVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((ODLC.ODLCType)value == ODLC.ODLCType.STANDARD)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    //Visibility converter for Emergent Target Fields
    public class EmergentTargetVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((ODLC.ODLCType)value == ODLC.ODLCType.STANDARD)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;

            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    //Converter used to enable controls when an item has been selected in the ListBox.
    public class ObjectSelectedToEnabledConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((int)value == -1)
                {
                    return false;
                }
                return true;

            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
