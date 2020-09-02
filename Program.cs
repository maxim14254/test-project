using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    class Program
    {
        private static string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        private static List<string> albums = new List<string>() { };
        private static string sql;
        private static SqlCommand command;
        private static bool block = true;//переменная открывающая проход для добавление артиста и его альбомов
        public static void Main(string[] args)
        {
            SqlConnection sqlConnection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\Database1.mdf;Integrated Security=True;Connect Timeout=30");
            sqlConnection.Open(); // подключаем базу данных

            WebClient wc = new WebClient(); // класс для загрузки файлов

            Console.Write("Введите название группы (исполнителя): ");
            string group = Console.ReadLine();// вводим название группы (исполнителя)

            try// пробуем загрузить файл по ссылке
            {
                wc.DownloadFile(String.Format("https://itunes.apple.com/search?term={0}&entity=musicArtist&limit=1", group), "artistId.txt");// загружаем и сохраняем файл в корень программы "artistId.txt" 
            }
            catch (System.Net.WebException)
            {
                Console.WriteLine("Нет сети! Проверте подключение\n");//если нет подключения, осуществляем загрузку данных и нашей БД
                sql = String.Format("SELECT albums.name FROM albums INNER JOIN groups ON albums.group_id = Groups.Id WHERE Groups.name = N'{0}'", group);
                command = new SqlCommand(sql, sqlConnection);

                SqlDataReader reader = command.ExecuteReader(); // сохраняем результаты поиска в объект reader

                while (reader.Read()) // перебираем
                {
                    Console.WriteLine(reader.GetValue(0).ToString());// выводим на экран
                }

                Console.ReadKey();
                StartProgram();//рестарт программы
            }

            string directory = System.IO.Path.GetDirectoryName(path) + "\\artistId.txt";// сохраняем путь загруженного файла

            StreamReader streamReader = new StreamReader(directory.ToString());// объект класса для работы с загруженным файлом
            string str = "";

            while (!streamReader.EndOfStream)//перебираем данные у загруженного файла (artistId.txt)
            {
                str += streamReader.ReadLine();
            }

            Match regex = Regex.Match(str, @".artistId.:(.*?),");// шаблон для поиска ID артиста в Itunes

            if (regex.Groups[1].Value == "")// если ID не обнаруживатся, то артиста нет а БД Itunes
            {
                Console.WriteLine("Такой группы (исполнителя) нет");
                Console.ReadKey();
                StartProgram();
            }

            try
            {
                wc.DownloadFile(String.Format(" https://itunes.apple.com/lookup?id={0}&entity=album", regex.Groups[1].Value), "albums.txt");// загружаем и сохраняем файл в корень программы "albums.txt"
                // в файле albums.txt указаны все альбомы артиста, находящиеся в БД Itunes
            }
            catch (Exception ex) when (ex is System.Net.WebException)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                StartProgram();
            }

            directory = System.IO.Path.GetDirectoryName(path) + "\\albums.txt";

            streamReader = new StreamReader(directory.ToString()); //объект класса для работы с загруженным файлом albums.txt
            str = "";

            while (!streamReader.EndOfStream)//перебираем данные у загруженного файла (albums.txt)
            {
                str += streamReader.ReadLine();
            }

            MatchCollection regex2 = Regex.Matches(str, @".collectionName.:.(.*?).,");// шаблон для поиска названий альбомов артиста в Itunes
            int i = 0;// переменная для подсчета кол-ва альбомов 

            foreach (Match m in regex2)// перебираем коллекцию совпадений, указанного выше шаблона
            {
                albums.Add(m.Groups[1].Value);// добавляем совпадение в List albums, 
                Console.WriteLine(m.Groups[1].Value);//выводим название альбома
                SetData(i, sqlConnection, group);// метод для сохранения в БД альбомов и артиста
                i++;
            }
            Console.ReadKey();
            StartProgram();

            sqlConnection.Close();
        }

        private static void StartProgram()
        {
            System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(path) + "\\ConsoleApp1.exe");// заново запускаем програаму
            Process.GetCurrentProcess().Kill();
        }
        private static void SetData(int i, SqlConnection sqlConnection, string group)
        {
            if (i == 0 && block) //выполняется 1 раз 
            {
                sql = String.Format("SELECT name FROM groups");//запрос на вывод всех артистов в БД
                command = new SqlCommand(sql, sqlConnection);

                SqlDataReader reader = command.ExecuteReader();//сохраняем всех артистов в объекте reader
                int a;
                while (reader.Read())
                {
                    a = String.Compare(group, reader.GetValue(0).ToString());// сравниваем артиста которого ввели и который находится в БД
                    if (a == 0)//если они совпали, то закрываем проход на добавление нового артиста в БД
                    {
                        block = false;
                        break;
                    }
                }

                reader.Close();

                if (block)// проход на добавление артиста в БД
                {
                    sql = String.Format("INSERT groups (name) VALUES (N'{0}')", group.Replace('\'', '`'));
                    command = new SqlCommand(sql, sqlConnection);
                    command.ExecuteNonQuery();
                }
            }

            if (block)// проход на добавление альбомов в БД
            {
                sql = String.Format("SELECT id FROM groups WHERE name = N'{0}'", group);//запрос на вывод ID артиста
                command = new SqlCommand(sql, sqlConnection);
                string id = command.ExecuteScalar().ToString();// сохраняем ID

                sql = String.Format("INSERT albums (name,group_id) VALUES (N'{0}','{1}')", albums[i].Replace('\'', '`'), id);//запрос на запись альбома[i] в БД
                command = new SqlCommand(sql, sqlConnection);
                command.ExecuteNonQuery();
            }
        }
    }

}

