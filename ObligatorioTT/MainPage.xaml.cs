using Microsoft.Maui.Media;
using System.Text.Json;
using System.Threading.Tasks;
namespace ObligatorioTT

{
    public partial class MainPage : ContentPage
    {
        
        int count = 0;

        public MainPage()
        {
            try
            {
                InitializeComponent();

                //List<String> list = new List<String>();
                //list.Add("Esto es una lista");
                //list.Add("o no es una lista?");
                //list.Add("si era");

                //string filename = FileSystem.AppDataDirectory + "/ArchivoJson.json";
                //var serializedData = JsonSerializer.Serialize(list);
                //File.WriteAllText(filename, serializedData);

                //var rawData = File.ReadAllText(filename);
                //var listaDez = JsonSerializer.Deserialize<List<String>>(rawData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR en MainPage: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw; 
            }
        }

    }

}
