using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static List<Results> DataJson = new List<Results>();//основной массив который хранит в себе результаты
        static DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(List<Results>));
        private static string path = System.Reflection.Assembly.GetExecutingAssembly().Location;// путь исполняемого файла (нужно для перезапуска программы)
        static string groupName = "";// переменная названия группы
        public static void Main(string[] args)
        {
            using (var file = new FileStream("data.json", FileMode.OpenOrCreate))// считываем кеш файл data.json (в нем хранятся все результаты)
            {
                try
                {
                    DataJson = json.ReadObject(file) as List<Results>;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.Write("Введите название группы (исполнителя): ");
            groupName = Console.ReadLine();// вводим название группы (исполнителя)


            DownloadFile(String.Format("https://itunes.apple.com/search?term={0}&entity=musicArtist&limit=1", groupName), Directory.GetCurrentDirectory() + "\\artistId.json").Wait();// загружаем файл artistId.json

            Object @object = JsonConvert.DeserializeObject<Object>(File.ReadAllText("artistId.json"));//десериализуем загруженный файл

            if (@object.resultCount == 0) // если ID не обнаруживатся, то артиста нет а БД Itunes
            {
                Console.WriteLine("Такой группы (исполнителя) нет");
                Console.ReadKey();
                StartProgram();
            }

            try
            {
                DownloadFile(String.Format("https://itunes.apple.com/lookup?id={0}&entity=album", @object.results[0].artistId), Directory.GetCurrentDirectory() + "\\albums.json").Wait();// загружаем файл albums.json
            }
            catch (Exception ex) when (ex is System.Net.WebException)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                StartProgram();
            }
            int a = JsonConvert.DeserializeObject<Album>(File.ReadAllText("albums.json")).resultCount - 1;// кол-во альбомов у исполнителя
            string[] albums = new string[a];// массив для хранения названия альбомов
            for (int i = 0; i < albums.Length; i++) 
            {
                albums[i] = JsonConvert.DeserializeObject<Album>(File.ReadAllText("albums.json")).results[i+1].collectionName;// передаем название альбома в массив
                Console.WriteLine(albums[i].ToString());
            }
            Results results = new Results { artistId = @object.results[0].artistId, collectionNames = albums, artistName = @object.results[0].artistName };// результирующий объект класса Results (все необходимые данные об исполнителе)
            SetData(results);// заносим полученные данные в КЕШ
            Console.ReadKey();
            StartProgram();
        }

        private static void StartProgram()
        {
            System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(path) + "\\ConsoleApp1.exe");// заново запускаем програаму
            Process.GetCurrentProcess().Kill();
        }
        private static void SetData(Results results)// метод сохранения полученного результатат в КЕШ
        {
            bool block = true;// переменная блокировки элемента кода
            for (int i = 0; i < DataJson.Count; i++) 
            {
                if (String.Compare(DataJson[i].artistName, results.artistName) == 0) // проверяем отсутствие введеного исполнителя в нашем КЕШ 
                {
                    block = false;//закрываем элемент кода
                    break;
                }
            }
            if (block)// если имя введеного исполнителя отсутствует в КЕШ
            {
                DataJson.Add(results);
                using (var file = new FileStream("data.json", FileMode.OpenOrCreate))// создаем или открывает файл data.json
                {
                    json.WriteObject(file, DataJson);// сериализация
                }
            } 
        }

        private static async Task DownloadFile(string url, string file)// метод загрузки файлов из библиотеки Itunes
        {
            try
            {
                WebClient wc = new WebClient(); // класс для загрузки файлов
                await wc.DownloadFileTaskAsync(new Uri(url), file);// загружаем и сохраняем файл в корень программы "artistId.txt" 
            }
            catch (Exception ex) when (ex is System.Net.WebException)
            {
                Console.WriteLine(ex.Message);//если нет подключения, осуществляем загрузку данных и нашего КЕШа
                for (int i = 0; i < DataJson.Count; i++)
                {
                    if (String.Compare(DataJson[i].artistName.ToLower(), groupName.ToLower()) == 0)// сравниваем введеное название группы и названия групп, находящихся в КЕШ
                    {
                        Console.WriteLine("{0}", string.Join("\n", DataJson[i].collectionNames));// выводим список всех альбомов
                        break;
                    }
                }
                Console.ReadKey();
                StartProgram();//рестарт программы
            }
        }
     
    }
   
}



