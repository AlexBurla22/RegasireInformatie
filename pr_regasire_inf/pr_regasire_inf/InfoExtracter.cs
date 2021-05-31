using Porter2Stemmer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace pr_regasire_inf
{
    class InfoExtracter
    {
        string[] stopwords;
        List<Dictionary<string, int>> dictionare_fisiere;
        List<string> cuv_unice;
        List<List<float>> matrice;
        EnglishPorter2Stemmer stemmer;
        Dictionary<string, int> queryDictionary;
        string[] files;
        Dictionary<string, float> fileQueryResult;
        int queryNumber;
        const int numberOfResultsReturned = 50;
        public InfoExtracter()
        {
            dictionare_fisiere = new List<Dictionary<string, int>>();
            cuv_unice = new List<string>();
            matrice = new List<List<float>>();
            stopwords = System.IO.File.ReadAllLines("Files\\english.txt");
            stemmer = new EnglishPorter2Stemmer();
            queryDictionary = new Dictionary<string, int>();
            Dictionary<string, float> fileQueryResult = new Dictionary<string, float>();
            queryNumber = 0;
        }

        public void ProcessFilesInDirectory(string dirPath, string fileExtension)
        {
            DirectoryInfo dir = new DirectoryInfo(dirPath);

            files = GetFileNames(dir.GetFiles(fileExtension));

            foreach (var file in files)
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(dir.FullName + "\\\\" + file);

                XmlNode title = doc.DocumentElement.SelectSingleNode("title");

                XmlNode text = doc.DocumentElement.SelectSingleNode("text");

                XmlNodeList paragrafe = text.SelectNodes("p");

                string allText = title.InnerText;
                foreach (XmlNode p in paragrafe)
                {
                    allText += Environment.NewLine + p.InnerText;
                }
                allText = allText.ToLower().Trim();

                AddFileWordDictionary(ExtractWordsFromText(allText));
            }
            CreateMatrix();
        }

        internal void QueryConsole(string q)
        {
            WriteResultToConsole(Query(q));
        }

        private static string[] GetFileNames(FileInfo[] files)
        {
            string[] s = new string[files.Length];
            int i = 0;
            foreach (var file in files)
            {
                s[i] = file.Name;
                i++;
            }
            return s;
        }
        private List<string> ExtractWordsFromText(string text)
        {
            List<string> words = InitialSplit(text);

            for (int i = 0; i < words.Count; i++)
            {   //remove acronyms
                if (Regex.IsMatch(words[i], @"\b(?:[a-zA-Z]\.){2,}"))
                {
                    string temp1 = null;
                    foreach (var charr in words[i])
                    {
                        if (charr != '.')
                        {
                            temp1 += charr;
                        }
                    }
                    words[i] = temp1;
                }
                //remove numbers
                words[i] = Regex.Replace(words[i], @"[\d-]", ".");
                //remove point
                if (words[i].Contains('.'))
                {
                    string[] temp2 = words[i].Split('.');
                    words[i] = " ";

                    foreach (var item in temp2)
                    {
                        words.Add(item);
                    }
                }
                //remove stopwords
                if (stopwords.Contains(words[i]))
                {
                    words[i] = " ";
                }
                //remove hyph
                words[i] = words[i].Replace("\'", "");
                //stem
                words[i] = stemmer.Stem(words[i]).Value;
            }
            words = words.Where(w => !string.IsNullOrWhiteSpace(w)).ToList(); //remove empty spaces

            return words;
        }
        private void AddFileWordDictionary(List<string> words)
        {
            Dictionary<string, int> frecventa = new Dictionary<string, int>();

            foreach (var word in words)
            {
                if (!frecventa.ContainsKey(word))
                {
                    frecventa.Add(word, 1);
                }
                else
                {
                    frecventa[word]++;
                }
                if (!cuv_unice.Contains(word))
                {
                    cuv_unice.Add(word);
                }
            }
            dictionare_fisiere.Add(frecventa);
        }
        private void CreateMatrix()
        {
            cuv_unice.Sort();
            foreach (Dictionary<string, int> dic in dictionare_fisiere)
            {
                List<float> rand = new List<float>();
                foreach (string cuv in cuv_unice)
                {
                    int val;
                    bool gasit = dic.TryGetValue(cuv, out val);
                    if (gasit)
                    {
                        rand.Add((float)val);
                    }
                    else
                    {
                        rand.Add((float)0.0);
                    }
                }
                matrice.Add(rand);
            }
            NormalizeMatrix();
        }
        private List<string> InitialSplit(string sir)
        {
            char[] sep = new char[] { ' ', ',', '!', '?', '\"', '\'', ':', ';',
                                      '(', ')', '/', '&', '-', '$', '=',
                                      '%', '#', '@', '^', '*', '|','\\', '\r',
                                      '\n', '+', '\t', '\v', '\f', '[', ']', '{', '}' , '_'};
            return sir.Split(sep, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        private List<KeyValuePair<string, float>> Query(string q)
        {
            fileQueryResult = new Dictionary<string, float>();
            queryDictionary = new Dictionary<string, int>();
            List<float> queryVector = CreateQueryVector(q);
            for (int i = 0; i < matrice.Count; i++)
            {
                float d = CalculDistanta(matrice[i], queryVector);
                fileQueryResult.Add(files[i], d);
            }
            var sortedFiles = fileQueryResult.OrderBy(d => d.Value).ToList();
            return sortedFiles;
        }
        public void QueryFile(string q)
        {  
            WriteResultToFile(Query(q));
        }
        private void WriteResultToFile(List<KeyValuePair<string, float>> sortedFiles)
        {
            string outPutFilePath = "Files\\Output\\QueryResult_";
            outPutFilePath += queryNumber;
            outPutFilePath += ".txt";
            TextWriter tw = new StreamWriter(outPutFilePath);
            for (int i=0; i < sortedFiles.Count && i < numberOfResultsReturned; i++)
            {
                tw.WriteLine(sortedFiles[i]);
            }
            tw.Close();
            queryNumber++;
        }
        private void WriteResultToConsole(List<KeyValuePair<string, float>> sortedFiles)
        {
            for (int i = 0; i < sortedFiles.Count && i < numberOfResultsReturned; i++)
            {
                Console.WriteLine(sortedFiles[i]);
            }
        }
        private float CalculDistanta(List<float> line, List<float> queryVector)
        {
            float sum = (float)0.0;
            //float norm_a = (float)0.0;
            //float norm_b = (float)0.0;

            for (int i = 0; i < line.Count; i++)
            {
                //sum += (float)Math.Abs(line[i] - queryVector[i]); //manhattan
                sum += (float)Math.Pow((line[i] - queryVector[i]), 2); //euclidiana
                /*
                //ab
                sum += line[i] * queryVector[i];
                //||a||
                norm_a += (float)Math.Pow(line[i], 2);
                //||b||
                norm_b += (float)Math.Pow(queryVector[i], 2);
                //cosine similarity*/
            }
           // norm_a = (float)Math.Sqrt(norm_a);
           // norm_b = (float)Math.Sqrt(norm_b);

            return (float)Math.Sqrt(sum); //euclidiana
            //return sum; //manhattan
            //return sum / (norm_a) * (norm_b);
        }
        private List<float> CreateQueryVector(string q)
        {
            CreateQueryDictionary(ExtractWordsFromText(q));
            List<float> qVector = new List<float>();
            foreach (string cuv in cuv_unice)
            {
                int val;
                if (queryDictionary.TryGetValue(cuv, out val))
                {
                    qVector.Add((float)val);
                }
                else
                {
                    qVector.Add((float)0.0);
                }
            }
            NormalizeVector(qVector);
            return qVector;
        }
        private void CreateQueryDictionary(List<string> words)
        {
            foreach (var word in words)
            {
                if (queryDictionary.ContainsKey(word))
                {
                    queryDictionary[word]++;
                }
                else
                {
                    queryDictionary.Add(word, 1);
                }
            }
        }
        private void NormalizeMatrix()
        {
            for (int i = 0; i < matrice.Count; i++)
            {
                NormalizeVector(matrice[i]);
            }
        }
        private void NormalizeVector(List<float> v)
        {
            //float max = v.Max();
            float sum = v.Sum();
            for (int i = 0; i < v.Count; i++)
            {
                if (v[i] != 0)
                {
                    //v[i] = 1; binara 
                    //v[i] = ((float)(1 + Math.Log(1 + Math.Log(v[i])))); //cornell 
                    //v[i] = v[i] / max; //nominala
                    v[i] = v[i] / sum; //suma 1
                }
            }
        }
    }
}
