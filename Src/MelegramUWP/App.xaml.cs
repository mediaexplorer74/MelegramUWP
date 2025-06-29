using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BackgroundTaskComponent;
using Microsoft.QueryStringDotNET;
using System.Linq;

namespace MelegramUWP
{
    sealed partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await OnLaunchedOrActivated(e);
        }

        /// <summary>
        /// Invoked when the application is activated by some means other than normal launching.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        protected override async void OnActivated(IActivatedEventArgs e)
        {
            await OnLaunchedOrActivated(e);
        }

        private async Task OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            // Initialize things like registering background task before the app is loaded
            await InitializeApp();


            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            // Handle toast activation
            if (e is ToastNotificationActivatedEventArgs)
            {
                var toastActivationArgs = e as ToastNotificationActivatedEventArgs;

                // If empty args, no specific action (just launch the app)
                if (toastActivationArgs.Argument.Length == 0)
                {
                    // DEBUG
                    if (1 == 1)//(rootFrame.Content == null)
                    {
                        //rootFrame.Navigate(typeof(MainPage));
                        rootFrame.Navigate(typeof(ImagePage));
                    }
                }

                // Otherwise an action is provided
                else
                {
                    // Parse the query string
                    QueryString args = QueryString.Parse(toastActivationArgs.Argument);

                    // See what action is being requested 
                    switch (args["action"])
                    {
                        // Open the image
                        case "viewImage":

                            // The URL retrieved from the toast args
                            string imageUrl = args["imageUrl"];

                            // If we're already viewing that image, do nothing
                            if (rootFrame.Content is ImagePage && (rootFrame.Content as ImagePage).ImageUrl.Equals(imageUrl))
                                break;

                            // Otherwise navigate to view it
                            rootFrame.Navigate(typeof(ImagePage), imageUrl);
                            break;


                        // Open the conversation
                        /*case "viewConversation":

                            // The conversation ID retrieved from the toast args
                            int conversationId = int.Parse(args["conversationId"]);

                            // If we're already viewing that conversation, do nothing
                            if (rootFrame.Content is ConversationPage && (rootFrame.Content as ConversationPage).ConversationId == conversationId)
                                break;

                            // Otherwise navigate to view it
                            rootFrame.Navigate(typeof(ConversationPage), conversationId);
                            break;
                            */

                        default:
                            //throw new NotImplementedException();
                            // Otherwise navigate to view it
                            rootFrame.Navigate(typeof(ImagePage));
                            break;
                    }

                    // If we're loading the app for the first time, place the main page on the back stack
                    // so that user can go back after they've been navigated to the specific page
                    if (rootFrame.BackStack.Count == 0)
                        rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
                }
            }

            // Handle launch activation
            else if (e is LaunchActivatedEventArgs)
            {
                var launchActivationArgs = e as LaunchActivatedEventArgs;

                // If launched with arguments (not a normal primary tile/applist launch)
                if (launchActivationArgs.Arguments.Length > 0)
                {
                    // TODO: Handle arguments for cases like launching from secondary Tile, so we navigate to the correct page
                    throw new NotImplementedException();
                }

                // Otherwise if launched normally
                else
                {
                    // If we're currently not on a page, navigate to the main page
                    if (rootFrame.Content == null)
                        rootFrame.Navigate(typeof(MainPage));
                }
            }

            else
            {
                // TODO: Handle other types of activation
                throw new NotImplementedException();
            }


            // Ensure the current window is active
            Window.Current.Activate();
        }


      


        private bool _isInitialized = false;
        private async Task InitializeApp()
        {
            if (_isInitialized)
                return;

            await SaveProfilePicToAppData();

            RegisterBackgroundTask();

            _isInitialized = true;
        }

        private void RegisterBackgroundTask()
        {
            const string taskName = "ToastBackgroundTask";

            // If background task is already registered, do nothing
            if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(taskName)))
                return;

            // Otherwise create the background task
            var builder = new BackgroundTaskBuilder()
            {
                Name = taskName,
                TaskEntryPoint = typeof(ToastNotificationBackgroundTask).FullName
            };

            // And set the toast action trigger
            builder.SetTrigger(new ToastNotificationActionTrigger());

            // And register the task
            builder.Register();

        }//

        private async Task SaveProfilePicToAppData()
        {
            // Realistically, this would probably come from the internet and then get cached in the app
            StorageFile profilePic = await StorageFile.GetFileFromApplicationUriAsync(new System.Uri("ms-appx:///Assets/Andrew.jpg"));

            // And now cache their profile pic in the app
            await profilePic.CopyAsync(ApplicationData.Current.LocalFolder, "Andrew.jpg", NameCollisionOption.ReplaceExisting);
        }// 



        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }


        /// <summary>
        /// Initialized root frame and navigates to the main page
        /// </summary>
        /// <param name="args"></param>
        private void InitFrame(IActivatedEventArgs args)
        {
            Frame rootFrame = GetRootFrame();
            //ThemeHelper.Initialize();

            rootFrame.Navigate(typeof(MainPage), "sync");
        }

        /// <summary>
        /// Gets the root frame. Used for setting the app theme at launch
        /// </summary>
        /// <returns>The root frame</returns>
        private Frame GetRootFrame()
        {
            Frame rootFrame;
            if (!(Window.Current.Content is MainPage rootPage))
            {
                rootPage = new MainPage();
                rootFrame = (Frame)rootPage.FindName("rootFrame");
                if (rootFrame == null)
                {
                    throw new Exception("Root frame not found");
                }
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootPage;
            }
            else
            {
                rootFrame = (Frame)rootPage.FindName("rootFrame");
            }

            return rootFrame;
        }

        protected async override void OnFileActivated(FileActivatedEventArgs args)
        {
            // TODO: Handle file activation
            // The number of files received is args.Files.Count
            // The name of the first file is args.Files[0].Name
            if (args.Files.Count > 0)
            {
                StorageFile storageFile = args.Files[0] as StorageFile;
                StorageFolder storageFolder = ApplicationData.Current.TemporaryFolder;
                await storageFolder.CreateFileAsync(storageFile.Name, CreationCollisionOption.ReplaceExisting);
                string json = await FileIO.ReadTextAsync(storageFile);
                var file = await storageFolder.GetFileAsync(storageFile.Name);
                await FileIO.WriteTextAsync(file, json);
                Frame rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;
                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }


                rootFrame.Navigate(typeof(/*ImportBacklog*/MainPage), file.Name);
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }
    }
}
