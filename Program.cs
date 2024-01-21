     using System;
     using System.Collections.Generic;
     using System.Data.SqlClient;
     using System.Net.Http;
     using System.Threading.Tasks;
     using Newtonsoft.Json;

      namespace ConsoleApplication2
     {
      class Program
     {
        // Chaîne de connexion à la base de données SQL Server
        private const String cs = @"Data Source=YASMIINA;Initial Catalog=ListesDepartements;Trusted_Connection=true";
        
        // URL de base de l'API des communes
        private const string baseURL = "https://geo.api.gouv.fr/communes/";
        // URL de fin pour la requête API (champs à inclure et format JSON)
        private const string endurl = "?fields=&format=json";

        // Méthode principale du programme
        static async Task Main(string[] args)
        {
            // Appel de la méthode asynchrone pour lire les données des communes
            await ReadDataCommune();
        }

        // Méthode asynchrone pour lire les données des communes depuis l'API
        private static async Task ReadDataCommune()
        {
            // Liste pour stocker les objets Ville
            List<Ville> villes = new List<Ville>();
            
            // Utilisation d'un objet HttpClient pour effectuer une requête GET à l'API
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(baseURL + endurl))
            {
                // Vérification de la réussite de la requête HTTP
                if (response.IsSuccessStatusCode)
                {
                    // Lecture du contenu JSON de la réponse
                    string json = await response.Content.ReadAsStringAsync();

                    // Désérialisation du JSON en une liste d'objets Ville
                    villes = JsonConvert.DeserializeObject<List<Ville>>(json);
                }
                else
                {
                    // Affichage d'une erreur en cas d'échec de la requête HTTP
                    Console.WriteLine("Erreur de requête : " + response.StatusCode);
                    return;
                }
            }

            // Boucle sur chaque objet Ville récupéré de l'API
            foreach (Ville ville in villes)
            {
                // Construction de l'URL spécifique pour chaque ville
                string uri = string.Concat(baseURL + ville.code + endurl);

                // Nouvelle requête pour obtenir des informations détaillées sur la ville
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    // Vérification de la réussite de la requête HTTP
                    if (response.IsSuccessStatusCode)
                    {
                        // Lecture du contenu JSON de la réponse
                        string json = await response.Content.ReadAsStringAsync();

                        // Désérialisation du JSON en un objet Ville
                        Ville item = JsonConvert.DeserializeObject<Ville>(json);

                        // Correction de la population à "0" si elle est nulle
                        if (item.population == null)
                        {
                            item.population = "0";
                        }

                        try
                        {
                            // Utilisation d'une connexion SQL pour mettre à jour la base de données
                            using (SqlConnection connection = new SqlConnection(cs))
                            {
                                // Ouverture de la connexion à la base de données
                                connection.Open();

                                // Requête SQL pour mettre à jour la population dans la table Commune
                                string query = "UPDATE [019HexaSmal] SET population = @population WHERE [Code_commune_INSEE] = @code";

                                SqlCommand command = new SqlCommand(query, connection);
                                command.Parameters.AddWithValue("@population", item.population);
                                command.Parameters.AddWithValue("@code", item.code);

                                // Exécution de la requête SQL et récupération du nombre de lignes affectées
                                int rowsAffected = command.ExecuteNonQuery();

                                // Affichage du résultat de la mise à jour
                                if (rowsAffected > 0)
                                {
                                    Console.WriteLine("Population de " + item.nom + " mise à jour.");
                                }
                                else
                                {
                                    Console.WriteLine("Aucune mise à jour pour la population de " + item.nom);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Affichage d'une erreur en cas d'échec de l'insertion dans la base de données
                            Console.WriteLine("Erreur lors de l'insertion dans la base de données : " + ex.Message);
                        }
                    }
                    else
                    {
                        // Affichage d'une erreur en cas d'échec de la requête HTTP pour une ville spécifique
                        Console.WriteLine("Erreur de requête pour la ville " + ville.nom + " : " + response.StatusCode);
                    }
                }
            }
        }
    }
}
