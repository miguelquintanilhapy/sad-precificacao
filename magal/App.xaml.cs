using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Markup; 
using System.Globalization;  
using magal.Services;

namespace magal
{

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Garante que todos os bindings de data, hora e moeda (R$) 
            // utilizem a configuração regional do Windows do usuário.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            // Rede de segurança: nenhum erro não tratado chega ao usuário com texto técnico.
            DispatcherUnhandledException += (s, args) =>
            {
                TratadorErros.Mostrar(args.Exception, "concluir a operação");
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex) TratadorErros.Mostrar(ex, "continuar a execução");
            };
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                TratadorErros.Registrar(args.Exception, "Falha em tarefa em segundo plano");
                args.SetObserved();
            };

            base.OnStartup(e);
        }
    }
}