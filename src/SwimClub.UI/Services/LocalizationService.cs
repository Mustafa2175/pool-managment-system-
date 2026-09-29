using System.Globalization;
using System.Threading;
using System.Windows;

namespace SwimClub.UI.Services;

public class LocalizationService
{
    public void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Force RTL/LTR based on language
        if (culture.TextInfo.IsRightToLeft)
        {
            System.Windows.Application.Current.MainWindow.FlowDirection = FlowDirection.RightToLeft;
        }
        else
        {
            System.Windows.Application.Current.MainWindow.FlowDirection = FlowDirection.LeftToRight;
        }
    }
}
