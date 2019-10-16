﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public MainWindow()
        {
            InitializeComponent();

            //Initialize the HttpClient object 
            gHttpClient = new HttpClient();
            gHttpClient.Timeout = new TimeSpan(0, 0, 10);
            gLoggedIn = false;

            Login();
            List<ODLC> temp = getOLDC();
            ODLC temp2 = getOLDC(1);
        }

        /// <summary>
        /// Function to log into the interop server using the supplied username and password.
        /// The HttpClient stores the cookie given by the server to use for future communication.
        /// </summary>
        private void Login()
        {
            //Set base address of the httpclient in case it was changed. 
            gHttpClient.BaseAddress = new Uri(Properties.Settings.Default.url);
            string login = string.Format("{{ \"username\":\"{0}\",\"password\":\"{1}\"}}", Properties.Settings.Default.username, Properties.Settings.Default.password);
            StringContent content = new StringContent(login, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = gHttpClient.PostAsync("/api/login", content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    gLoggedIn = true;
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

            return;
        }

        /// <summary>
        /// Uploads a new ODLC object to the server. Returns the uploaded object. 
        /// </summary>
        /// <param name="odlcObject"></param>
        /// <returns></returns>
        private ODLC postODLC(ODLC odlcObject)
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
                HttpResponseMessage response = gHttpClient.PostAsync("/api/odlcs", content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    odlcResponse = JsonConvert.DeserializeObject<ODLC>(response.Content.ReadAsStringAsync().Result);
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
        private ODLC updateODLC(ODLC odlcObject)
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
                HttpResponseMessage response = gHttpClient.PutAsync("/api/odlcs/" + id, content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    odlcResponse = JsonConvert.DeserializeObject<ODLC>(response.Content.ReadAsStringAsync().Result);
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

        /// <summary>
        /// Gets a list of ODLC targets uploaded to the server. Returns NULL from any server errors. 
        /// </summary>
        /// <returns></returns>
        private List<ODLC> getOLDC()
        {
            if (!gLoggedIn)
            {
                return null;
            }
            List<ODLC> odlcList = null;
            try
            {
                HttpResponseMessage response = gHttpClient.GetAsync("/api/odlcs").Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    odlcList = JsonConvert.DeserializeObject<List<ODLC>>(response.Content.ReadAsStringAsync().Result);
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
                HttpResponseMessage response = gHttpClient.GetAsync("/api/odlcs/" + id.ToString()).Result;
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

        /// <summary>
        /// Class that holds the OLDC object. 
        /// </summary>
        private class ODLC
        {
            //We cannot send an id when uploading a new object. We only set ID when updating the object. 
            [JsonProperty("mission", NullValueHandling = NullValueHandling.Ignore)]
            private int? id;

            public int? ID { get; }

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
                    mission = value;
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
                    type = value;
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
                    latitude = value;
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
                    longitude = value;
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
                    orientation = value;
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
                    shape = value;
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
                    shapeColor = value;
                }
            }

            [JsonProperty("alphanumeric_color")]
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
                    alphanumericColor = value;
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
                    description = value;
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
                    autonomous = value;
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
                shapeColor = ODLCColor.BLACK;
                alphanumericColor = ODLCColor.BLACK;
                description = "";
                autonomous = false;
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
        }

    }
}
