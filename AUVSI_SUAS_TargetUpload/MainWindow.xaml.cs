using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Http;
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
        }

        public Exception Ex { get; private set; }

        /// <summary>
        /// Function to log into the interop server using the supplied username and password.
        /// The HttpClient stores the cookie given by the server to use for future communication.
        /// </summary>
        private async void Login()
        {
            //Set base address of the httpclient in case it was changed. 
            gHttpClient.BaseAddress = new Uri(Properties.Settings.Default.url);
            string login = string.Format("{{ \"username\":\"{0}\",\"password\":\"{1}\"}}", Properties.Settings.Default.username, Properties.Settings.Default.password);
            StringContent content = new StringContent(login, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await gHttpClient.PostAsync("/api/login", content);
                if(response.StatusCode == HttpStatusCode.OK)
                {
                    gLoggedIn = true;
                }
                else if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //Username is incorrect
                    MessageBox.Show("The provided username and password is incorrect. Please verify and try again", "Invalid Username or Password", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                Exception exception = ex;
                string message = "An error occured when attempting to connect to the server.\r\nPlease verify the correct URL is specified.\r\n\r\nException Messages:\r\n";
                while(true)
                {
                    message += exception.Message + "\r\n";
                    if(exception.InnerException != null)
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
            catch(ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Error");
            }
            
            return;
        }

    }
}
