using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Theblueway.Core.Runtime;
using Theblueway.Core.Runtime.UI.PageNavigation;
using Theblueway.SaveAndLoad.Samples.Packages.com.blueutils.saveandload.Samples.Scripts.UI.PageNavitgationSpecifications;
using UnityEngine;


namespace Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI
{

    public class LoadWorldSaveUI : MonoBehaviour, ILoadWorldSaveUINavigator
    {
        public WorldElement _worldElementPrefab;
        public Transform _worldListContainer;

        public List<FileInfo> __foundWorldSaves;

        public UIPageNavigationCommand<MainMenuUIPagNavigationParams> EnterNewWorldPageCommand { get; set; }

        public UIPageNavigationCommand<MainMenuUIPagNavigationParams> EnterLoadWorldPageCommand { get; set; }




        //used by UnityEvent in inspector
        public void Open()
        {
            gameObject.SetActive(true);


            string folderPath = Paths.Singleton.WorldSavePath;

            if (!Directory.Exists(folderPath))
            {
                return;
            }

            __foundWorldSaves =
                //Directory.GetFiles(folderPath, "*.json")
                Directory.GetFiles(folderPath)
                         .Select(path => new FileInfo(path))
                         .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                         .ToList();

            for (int i = 0; i < 10; i++)
            {
                var file = __foundWorldSaves[i];

                var element = Instantiate(_worldElementPrefab, _worldListContainer);


                byte[] headerBytes = FileUtil.ReadHeader(file.FullName, relative: false);

                if (headerBytes.IsNotNullAndNotEmpty())
                {
                    string json = System.Text.Encoding.UTF8.GetString(headerBytes);

                    SaveFileHeader header = JsonConvert.DeserializeObject<SaveFileHeader>(json);

                    element._levelName.text = header.levelName;

                    if (header.tankNames.IsNotNullAndNotEmpty())
                        element._tankNames.text = string.Join(", ", header.tankNames);
                    else
                        element._tankNames.text = "";
                }
                else
                {
                    element._levelName.text = "No data found";
                    element._tankNames.text = "";
                }


                int index = i; // Capture the current index for the listener

                element._worldNameText.text = file.Name;
                element._selectWorldButton.onClick.AddListener(() => OnWorldMenuItemClicked(index));
            }
        }


        //used by UnityEvent in inspector
        public void Close()
        {
            gameObject.SetActive(false);
            //_worldListContainer.DestroyAllChildren();
        }

        protected virtual void OnWorldMenuItemClicked(int i)
        {
            FileInfo fileInfo = __foundWorldSaves[i];

            MyGameManager.Singleton.LoadSavedWorld(fileInfo.FullName);
            //MySceneManager.Singleton.LoadWorld(fileInfo.FullName);
        }

        public void YieldControl(MainMenuUIPagNavigationParams navigationParams)
        {
            throw new System.NotImplementedException();
        }

        public void GainControl(MainMenuUIPagNavigationParams navigationParams)
        {
            Open();
        }

        public void TakeBackControl(MainMenuUIPagNavigationParams navigationParams)
        {
            throw new System.NotImplementedException();
        }

        public void ReturnControl(MainMenuUIPagNavigationParams navigationParams)
        {
            Close();
        }
    }


    public class SaveFileHeader
    {
        public string levelName;
        public string[] tankNames;
    }


}