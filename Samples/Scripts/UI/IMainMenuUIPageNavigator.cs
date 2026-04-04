
using Theblueway.Core.Runtime.UI.PageNavigation;
using Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI;

namespace Theblueway.SaveAndLoad.Samples.Scripts.UI
{
    public interface IMainMenuUIPageNavigator:IPageNavigator<MainMenuUIPagNavigationParams>
    {
        public UIPageNavigationCommand<MainMenuUIPagNavigationParams> EnterNewWorldPageCommand { get; }
        public UIPageNavigationCommand<MainMenuUIPagNavigationParams> EnterLoadWorldPageCommand { get; }
    }
}
