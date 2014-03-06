using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;
using VDS.RDF;
using VDS.RDF.Ontology;
using Google.API.Search;



/// <summary>
/// Summary description for Service
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
// [System.Web.Script.Services.ScriptService]
public class Service : System.Web.Services.WebService {

    public Service () {


        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    //Returns the Stemming
    public class RootObject
    {
        // public string text { get; set; }
        //  public string porter_stem { get; set; }
        public string uea_stem { get; set; }
        // public string snowball_stem { get; set; }
    }

    //Retreives images from google
    private static IList<IImageResult> SearchImages(String keyword, int count)
    {
        GimageSearchClient client = new GimageSearchClient("http://www.google.com");
        IList<IImageResult> results = client.Search(keyword, count);
       
        return results;
    }
    [WebMethod]
    public String get()
    {
        string path = Server.MapPath("TextFiles");
        return path; 

    }
    [WebMethod]
    public String[] PrintString(string lessonScript) {


        //Set the path

        string path = Server.MapPath("TextFiles");
        Console.WriteLine(path);

        //Read Lesson Script+Remove Reg.Exp+Save Clean File
        string text = lessonScript;// File.ReadAllText(path + "\\Test.txt");
        string cleanText = Regex.Replace(text, @"[^\w\s]", "");
        File.WriteAllText(path + "\\CleanText.txt", cleanText);


        var client = new WebClient();
        using (StreamReader reader = new StreamReader(path + "\\CleanText.txt"))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {


                String response = client.DownloadString("http://www.enclout.com/stemmer/show.json?auth_token=5maBgHNmDRYTpHQ9pMV4&text=" + line);
                Console.WriteLine(JsonConvert.DeserializeObject<RootObject>(response).uea_stem);

                using (TextWriter Writer = new StreamWriter(path + "\\Stemmed.txt", true))
                {
                    Writer.WriteLine(JsonConvert.DeserializeObject<RootObject>(response).uea_stem);
                    line.Remove(line.Length - 1);
                }//Using StreamWriter

                Thread.Sleep(2000);

            }//While


           

        }//Using StreamReader

        //To make words text file
        string sText = File.ReadAllText(path + "\\Stemmed.txt");
        string[] words = sText.Split(' ');
        foreach (string word in words)
        {

            File.WriteAllLines(path + "\\Words.txt", words);

        }

        //Remove unneccesary files
         File.Delete(path + "\\Stemmed.txt");
         File.Delete(path + "\\CleanText.txt");


        //Regex Replacer = new Regex("(\b?:is|are|\ba\r|A|has|\ra\b)");
        Regex Replacer = new Regex(@"\b(?:is|are|has|a|A)\b");
        using (TextWriter Writer = new StreamWriter(path + "\\Words2.txt"))
        {
            using (StreamReader Reader = new StreamReader(path + "\\Words.txt"))
            {
                while (!Reader.EndOfStream)
                {
                    String line = Reader.ReadLine();
                    line = Replacer.Replace(line, "");
                    Writer.WriteLine(line);
                }
            }

            Writer.Flush();
        }

        var lines = File.ReadAllLines(path + "\\Words2.txt").Where(arg => !string.IsNullOrWhiteSpace(arg));
        File.WriteAllLines(path + "\\Words2.txt", lines);


        List<IUriNode> lsOfClasses = new List<IUriNode>();
        List<IUriNode> lsOfAttributes = new List<IUriNode>();
        List<String> lsOfC = new List<String>();
        List<String> lsOfA = new List<String>();
        List<String> lsOfWords = new List<String>();
        List<int> indexes = new List<int>();
        List<String> candidates = new List<String>();

        //Load the ontology graph
        OntologyGraph g = new OntologyGraph();
        g.LoadFromFile(path + "\\Shapes.rdf");

        OntologyClass someClass = g.CreateOntologyClass(new Uri("http://www.semanticweb.org/mjay/ontologies/2014/0/untitled-ontology-35#Shape"));
        // OntologyProperty topObjectProperty = g.CreateOntologyProperty(new Uri("http://www.semanticweb.org/mjay/ontologies/2014/0/untitled-ontology-35#topObjectProperty"));

        // Classes
        foreach (OntologyClass c in someClass.SubClasses)
        {
            int x = 0;
            x = c.Resource.ToString().IndexOf("#");
            lsOfC.Add(c.Resource.ToString().Remove(0, x + 1));
            lsOfClasses.Add(g.CreateUriNode(new Uri(c.Resource.ToString())));

        }

        //Attributes
        foreach (OntologyProperty c in g.OwlObjectProperties)
        {
            int x = 0;
            x = c.Resource.ToString().IndexOf("#");
            lsOfA.Add(c.Resource.ToString().Remove(0, x + 1));
            lsOfAttributes.Add(g.CreateUriNode(new Uri(c.Resource.ToString())));
        }



        //Creates Bool 2dArray    "S:Classes P:Attributes O:Value"
        Boolean[,] array2d = new Boolean[lsOfClasses.Count, lsOfAttributes.Count];
        for (int i = 0; i < lsOfClasses.Count; i++)
        {
            for (int j = 0; j < lsOfAttributes.Count; j++)
            {
                foreach (Triple t in g.GetTriplesWithSubjectPredicate(lsOfClasses.ElementAt(i), lsOfAttributes.ElementAt(j)))
                {
                    if (t.HasObject(t.Object))
                        array2d[i, j] = true;
                    else
                        array2d[i, j] = false;
                }
                


            }//for j
           
        }//for i

        //Put the words into a String for comparison
        String[] wordss = File.ReadAllLines(path +"\\Words2.txt");
        foreach (String w in wordss)
        {
            lsOfWords.Add(w);
        }


        // Compare with Attributes
        foreach (String s in lsOfA)
        {
            for (int i = 0; i < lsOfWords.Count; i++)
            {

                Boolean contains = s.IndexOf(lsOfWords.ElementAt(i), StringComparison.OrdinalIgnoreCase) >= 0;
                Console.WriteLine(s + " Contains: " + contains + " " + lsOfWords.ElementAt(i));
                if (contains)
                {
                    indexes.Add(lsOfA.IndexOf(s));
                }
            }
        }
        



        //Searches the indexes for the candidates
        for (int i = 0; i < lsOfC.Count; i++)
        {

            for (int k = 0; k < indexes.Count; k++)
            {
                if (array2d[i, indexes.ElementAt(k)] == true)
                {
                    candidates.Add(lsOfC.ElementAt(i));

                }
                else break;
            }

        }


        //Gets the Answer
        Dictionary<string, int> counts = candidates.GroupBy(x => x).ToDictionary(g2 => g2.Key, g2 => g2.Count());
        var Answer = counts.Keys.Max();

        var results = SearchImages(Answer, 5);
        String [] arrayURLs = new String[5];
      
     
       
          for (int i = 0; i < arrayURLs.Length; i++)
          arrayURLs[i] = results.ElementAt(i).TbImage.ToString() ;
                
                
            
            
       
       // return results.ElementAt(2).Url;
        return arrayURLs;
             

        



    }
    
}
