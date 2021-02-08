using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CopiaDocs
{
    public class CopiaArquivos
    {
        public static void Upload(DriveService servico, string[] ArquivosOrigem, string DiretorioDestino)
        {
            var diretorio = new Google.Apis.Drive.v3.Data.File();
            diretorio.Name = DiretorioDestino;
            diretorio.MimeType = "application/vnd.google-apps.folder";
            var idPasta = ProcurarArquivoId(servico, diretorio.Name).First();
            
            Console.WriteLine("Arquivos Copiados:");
            int i;
            for (i = 0; i < ArquivosOrigem.Length; i++)
            {
                var arquivo = new Google.Apis.Drive.v3.Data.File();
                arquivo.Name = Path.GetFileName(ArquivosOrigem[i]);
                Console.WriteLine("..." + arquivo.Name + " - " + ArquivosOrigem[i]);
                arquivo.MimeType = MimeTypes.GetMimeType(Path.GetExtension(ArquivosOrigem[i]));
                arquivo.Parents = new List<string>(new string[] { idPasta });
                using (var stream = new FileStream(ArquivosOrigem[i], FileMode.Open, FileAccess.Read))
                {
                    var ids = ProcurarArquivoId(servico, arquivo.Name);

                    Google.Apis.Upload.ResumableUpload<Google.Apis.Drive.v3.Data.File, Google.Apis.Drive.v3.Data.File> request;

                    if (ids == null || !ids.Any())
                    {
                        var theRequest = servico.Files.Create(arquivo, stream, arquivo.MimeType);
                        theRequest.Fields = "ids, parents";
                        request = theRequest;
                    }
                    else
                    {
                        var theRequest = servico.Files.Update(arquivo, ids.First(), stream, arquivo.MimeType);
                        theRequest.Fields = "ids, parents";
                        request = theRequest;
                    }
                    request.Upload();
                }
            }
            Console.WriteLine("Arquivos enviados com sucesso!!!");
            Console.ReadKey();
        }

        private static string[] ProcurarArquivoId(DriveService servico, string nome)
        {
            var retorno = new List<string>();
            var request = servico.Files.List();
            request.Q = string.Format("name = '{0}'", nome);
            request.Fields = "files(id)";
            request.Q = "trashed=false";
            var resultado = request.Execute();
            var arquivos = resultado.Files;

            if (arquivos != null && arquivos.Any())
            {
                foreach (var arquivo in arquivos)
                {
                    retorno.Add(arquivo.Id);
                }
            }
            return retorno.ToArray();
        }

        private static string[] BuscaArquivos(string caminho)
        {
            var lista = new List<string>();
            DirectoryInfo Dir = new DirectoryInfo(caminho);
            FileInfo[] Files = Dir.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo File in Files)
            {
                string FileName = File.FullName.Replace(Dir.FullName, "");
                lista.Add(Dir + FileName);
            }
            return lista.ToArray();
        }
   
        public static void Envia()
        {
            var credencial = Autenticar();
            using (var servico = AbrirServico(credencial))
            {
                Upload(servico, BuscaArquivos(@"C:\teste"), "teste");
            }
        }

        private static UserCredential Autenticar()
        {
            UserCredential credenciais;
            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                var diretorioAtual = Environment.CurrentDirectory;
                var diretorioCredenciais = Path.Combine(diretorioAtual, "credential");
                credenciais = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.Scope.Drive }, "user", CancellationToken.None,
                       new FileDataStore(diretorioCredenciais, true)).Result;
            }
            return credenciais;
        }

        private static DriveService AbrirServico(UserCredential credenciais)
        {
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credenciais
            });
        }
    }
}
