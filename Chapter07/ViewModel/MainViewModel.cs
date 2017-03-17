using Chapter7.Interface;
using Chapter7.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using System.Net.Http;
using Chapter7.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Text;

namespace Chapter7.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private WebRequest _webRequest;

        private ObservableCollection<RecommandationModel> _availableModels = new ObservableCollection<RecommandationModel>();
        public ObservableCollection<RecommandationModel> AvailableModels
        {
            get { return _availableModels; }
            set
            {
                _availableModels = value;
                RaisePropertyChangedEvent("AvailableModels");
            }
        }

        private RecommandationModel _selectedModel;
        public RecommandationModel SelectedModel
        {
            get { return _selectedModel; }
            set
            {
                _selectedModel = value;
                RaisePropertyChangedEvent("SelectedModel");
            }
        }

        private ObservableCollection<Product> _availableProducts = new ObservableCollection<Product>();
        public ObservableCollection<Product> AvailableProducts
        {
            get { return _availableProducts; }
            set
            {
                _availableProducts = value;
                RaisePropertyChangedEvent("AvailableProducts");
            }
        }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                _selectedProduct = value;
                RaisePropertyChangedEvent("SelectedProduct");
            }
        }

        private string _recommendations;
        public string Recommendations
        {
            get { return _recommendations; }
            set
            {
                _recommendations = value;
                RaisePropertyChangedEvent("Recommendations");
            }
        }

        public ICommand RecommendCommand { get; private set; }

        /// <summary>
        /// MainViewModel constructor. Creates a <see cref="WebRequest"/> object and command property
        /// </summary>
        public MainViewModel()
        {
            _webRequest = new WebRequest("https://westus.api.cognitive.microsoft.com/recommendations/v4.0/models/", "API_KEY_HERE");
            RecommendCommand = new DelegateCommand(RecommendProduct, CanRecommendBook);

            Initialize();
        }

        /// <summary>
        /// Initializing the view model by getting models and products
        /// </summary>
        private async void Initialize()
        {
            await GetModels();
            GetProducts();
        }

        /// <summary>
        /// Funciton to call the Recommendation API to retrieve all available <see cref="RecommandationModels"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetModels()
        {
            RecommandationModels models = await _webRequest.MakeRequest<object, RecommandationModels>(HttpMethod.Get, string.Empty);

            foreach (RecommandationModel model in models.models)
            {
                AvailableModels.Add(model);
            }
            
            SelectedModel = AvailableModels.FirstOrDefault();
        }

        /// <summary>
        /// Function to get all products in a given, hardcoded catalog
        /// Reads through a csv file and creates new a <see cref="Product"/> for each line
        /// </summary>
        private void GetProducts()
        {
            try
            {
                var reader = new StreamReader(File.OpenRead("catalog.csv"));

                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    var productInfo = line.Split(',');

                    AvailableProducts.Add(new Product(productInfo[0], productInfo[1], productInfo[2]));
                }

                SelectedProduct = AvailableProducts.FirstOrDefault();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Function to determine whether or not recommandation can happen
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if a recommendation model and product has been selected, false otherwise</returns>
        private bool CanRecommendBook(object obj)
        {
            return SelectedModel != null && SelectedProduct != null;
        }

        /// <summary>
        /// Command function for executing the recommendation
        /// Creates a query string, makes a request, and parses the result, into a string
        /// </summary>
        /// <param name="obj"></param>
        private async void RecommendProduct(object obj)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            
            queryString["itemIds"] = SelectedProduct.Id;
            queryString["numberOfResults"] = "10";
            queryString["minimalScore"] = "0";
            // queryString["includeMetadata"] = "";
            // queryString["buildId"] = "";

            Recommendations recommendations = await _webRequest.MakeRequest<object, Recommendations>(HttpMethod.Get, $"{SelectedModel.id}/recommend/item?{queryString.ToString()}");

            if(recommendations.recommendedItems.Length == 0)
            {
                Recommendations = "No recommendations found";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Recommended items:\n\n");

            foreach(Recommendeditem recommendedItem in recommendations.recommendedItems)
            {
                sb.AppendFormat("Score: {0}\n", recommendedItem.rating);

                foreach(string reason in recommendedItem.reasoning)
                {
                    sb.AppendFormat("Reason: {0}\n", reason);
                }

                foreach(Item item in recommendedItem.items)
                {
                    sb.AppendFormat("Item ID: {0}\nItem Name: {1}\n", item.id, item.name);
                }

                sb.Append("\n");
            }

            Recommendations = sb.ToString();
        }
    }
}