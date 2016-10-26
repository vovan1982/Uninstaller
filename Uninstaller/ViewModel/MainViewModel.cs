using System;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Uninstaller.Utils;
using Uninstaller.Model;
using Microsoft.Win32;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;

namespace Uninstaller.ViewModel
{
    //public static class DispatchService
    //{
    //    public static void Invoke(Action action)
    //    {
    //        Dispatcher dispatchObject = Application.Current.Dispatcher;
    //        if (dispatchObject == null || dispatchObject.CheckAccess())
    //        {
    //            action();
    //        }
    //        else
    //        {
    //            dispatchObject.Invoke(action);
    //        }
    //    }
    //    public static void Invoke(Action action)
    //    {
    //        if (Application.Current != null)
    //        {
    //            Dispatcher dispatchObject = Application.Current.Dispatcher;
    //            if (dispatchObject == null || dispatchObject.CheckAccess())
    //            {
    //                action();
    //            }
    //            else
    //            {
    //                dispatchObject.Invoke(action);
    //            }
    //        }
    //        else
    //        {
    //            action();
    //        }
    //    }
    //}
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Поля
        private ICommand _on_btClose_click; // Команда закрытия приложения
        private ICommand _on_btDelSoft_click; // Команда удаления выбранного ПО
        private ICommand _btConnectToRemPC_click; // Команда подключения к удаленному компьютеру
        private ICommand _sortCommand; // Команда сотрировки списка ПО
        private ICommand _copyUninstallStrCommand; // Команда копирования строки удаления выбранного ПО
        private ICommand _copyNameCommand; // Команда копирования имени выбранного ПО
        private string _textTitle; // Заголовок приложения с указанием имени ПК чьё ПО отображается
        private string _version; // Текущая версия приложения
        private string _login; // Логин учетной записи с правами администратора
        private string _password; // Пароль учетной записи с правами администратора
        private string _filterText; // Текст поля быстрого фильтра
        private string _statusText; // Текст строки состояния
        private ObservableCollection<Soft> _installedSoft; // Список установленного ПО
        private CollectionViewSource _installedSoftView; // Представление списка установленного ПО
        private string _sortColumn; // Колонка по которой выполняется сортировка
        private ListSortDirection _sortDirection; // Направление сортировки в колонке
        private Soft _selectedListItem; // Текущее выбранное ПО в списке
        private int _selectedListIndex; // Индекс текущего выбранного ПО в списке
        private readonly SynchronizationContext _syncContext; // UI поток для работы с основным интерфейсом
        private ImageSource _defaultIcon; // Иконка по умолчанию
        private bool _isRemoteMode; // Режим подключения к удалённому коммпьютеру
        private string _remoteHost; // Удалённый компьютер
        private bool _isEnableLogin; // Доступность поля логин
        private bool _isEnablePass; // Доступность поля пароль
        private bool _isEnableSelectPC; // Доступность группы Компьютер
        private bool _isEnableSearchFiel; // Доступность поля Поиск
        #endregion

        #region Свойства
        // Нажата кнопка закрытия окна приложения
        public ICommand On_btClose_click
        {
            get
            {
                return _on_btClose_click ?? (_on_btClose_click = new RelayCommand(arg => 
                {
                    Application.Current.Shutdown();
                }));
            }
        }
        // Нажата кнопка удаления выбранного ПО
        public ICommand On_btDelSoft_click
        {
            get 
            {
                return _on_btDelSoft_click ?? (_on_btDelSoft_click = new RelayCommand(arg =>
                {
                    RunDelSoft();
                }));
            }
            
        }
        // Нажата кнопка подключения к удалённому ПК
        public ICommand BtConnectToRemPC_click
        {
            get 
            {
                return _btConnectToRemPC_click ?? (_btConnectToRemPC_click = new RelayCommand(arg =>
                {
                    ConnectToRemPC();
                }));
            }
        }
        // Запущена сортировка списка ПО
        public ICommand SortCommand 
        {
            get
            {
                return _sortCommand ?? (_sortCommand = new RelayCommand(arg =>
                    {
                        Sort(arg);
                    }));
            } 
        }
        // Событие копирования строки удаления выбранного ПО
        public ICommand CopyUninstallStrCommand
        {
            get 
            {
                return _copyUninstallStrCommand ?? (_copyUninstallStrCommand = new RelayCommand(arg =>
                {
                    // Копируем в буфер строку удаления
                    Clipboard.SetDataObject(SelectedListItem.UninstallString);
                })); 
            }
        }
        // Событие копирования имени выбранного ПО
        public ICommand CopyNameCommand
        {
            get
            {
                return _copyNameCommand ?? (_copyNameCommand = new RelayCommand(arg =>
                {
                    // Копируем в буфер Название ПО
                    Clipboard.SetDataObject(SelectedListItem.Name);
                }));
            }
        }
        // Заголовок приложения с указанием имени ПК чьё ПО отображается
        public string TextTitle
        {
            get { return _textTitle; }
            private set {
                if (_textTitle != value)
                {
                    _textTitle = value;
                    OnPropertyChanged("TextTitle");
                }
            }
        }
        // Текущая версия приложения
        public string Version
        {
            get { return _version; }
            private set 
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged("Version");
                }
            }
        }
        // Логин учетной записи с правами администратора
        public string Login
        {
            get { return _login; }
            set {
                if (_login != value)
                {
                    _login = value;
                    OnPropertyChanged("Login");
                }
            }
        }
        // Пароль учетной записи с правами администратора
        public string Password
        {
            get { return _password; }
            set 
            { 
                _password = value;
            }
        }
        // Текст поля быстрого фильтра
        public string FilterText
        {
            get 
            { 
                return _filterText; 
            }
            set 
            { 
                _filterText = value;
                _installedSoftView.View.Refresh();
                InstalledSoftView.MoveCurrentToFirst();
                OnPropertyChanged("FilterText");
            }
        }
        // Текст строки состояния
        public string StatusText
        {
            get { return _statusText; }
            set 
            { 
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged("StatusText");
                }
            }
        }
        // Список установленного ПО
        public ObservableCollection<Soft> InstalledSoft 
        { 
            get
            {
                return _installedSoft;
            }
            set
            {
                if (value != null)
                {
                    _installedSoft = value;
                    _installedSoftView = new CollectionViewSource();
                    _installedSoftView.Source = _installedSoft;
                    _installedSoftView.Filter += installedSoft_Filter;
                    _installedSoftView.View.CurrentChanged += ListViewSelectionChanged;
                }
                else
                {
                    _installedSoft = value;
                    _installedSoftView = null;
                    //_installedSoftView = new CollectionViewSource();
                }
                OnPropertyChanged("InstalledSoft");
                OnPropertyChanged("InstalledSoftView");
            }
        }
        // Представление списка установленного ПО
        public ListCollectionView InstalledSoftView
        {
            get
            {
                if (_installedSoftView == null)
                    return null;
                else
                    return (ListCollectionView)_installedSoftView.View;
            }
        }
        // Текущее выбранное ПО в списке
        public Soft SelectedListItem
        {
            get 
            { 
                return _selectedListItem; 
            }
            set 
            {
                if (_selectedListItem != value)
                {
                    _selectedListItem = value;
                    OnPropertyChanged("SelectedListItem");
                }
            }
        }
        // Индекс текущего выбранного ПО в списке
        public int SelectedListIndex
        {
            get 
            { 
                return _selectedListIndex; 
            }
            set 
            {
                if (_selectedListIndex != value)
                {
                    _selectedListIndex = value;
                    OnPropertyChanged("SelectedListIndex");
                    OnPropertyChanged("DelButtonIsEnable");
                }
            }
        }
        // Доступность кнопки удаления ПО в зависимости от выбранного элемента списка ПО
        public bool DelButtonIsEnable
        {
            get
            {
                return !_isRemoteMode && InstalledSoftView != null && (SelectedListIndex >= 0 && InstalledSoftView.Count > 0);
            }
        }
        // Режим подключения к удалённому коммпьютеру
        public bool IsRemoteMode
        {
            get { return _isRemoteMode; }
            set 
            { 
                if (_isRemoteMode != value)
                {
                    _isRemoteMode = value;
                    OnPropertyChanged("IsRemoteMode");
                    OnPropertyChanged("IsEnableRemoteHostName");
                }
            }
        }
        // Доступность поля ввода удалённого компьютера
        public bool IsEnableRemoteHostName
        {
            get
            {
                return _isRemoteMode;
            }
        }
        // Имя удалённого компьютера
        public string RemoteHost
        {
            get { return _remoteHost; }
            set 
            { 
                if (_remoteHost != value)
                {
                    _remoteHost = value;
                    OnPropertyChanged("RemoteHost");
                }
            }
        }
        // Доступность поля пароль
        public bool IsEnablePass
        {
            get { return _isEnablePass; }
            set 
            { 
                if (_isEnablePass != value)
                {
                    _isEnablePass = value;
                    OnPropertyChanged("IsEnablePass");
                }
            }
        }
        // Доступность поля логин
        public bool IsEnableLogin
        {
            get { return _isEnableLogin; }
            set 
            { 
                if (_isEnableLogin != value)
                {
                    _isEnableLogin = value;
                    OnPropertyChanged("IsEnableLogin");
                }
            }
        }
        // Доступность группы Компьютер
        public bool IsEnableSelectPC
        {
            get { return _isEnableSelectPC; }
            set 
            { 
                if (_isEnableSelectPC != value)
                {
                    _isEnableSelectPC = value;
                    OnPropertyChanged("IsEnableSelectPC");
                }
            }
        }
        // Доступность поля Поиск
        public bool IsEnableSearchFiel
        {
            get { return _isEnableSearchFiel; }
            set 
            { 
                if (_isEnableSearchFiel != value)
                {
                    _isEnableSearchFiel = value;
                    OnPropertyChanged("IsEnableSearchFiel");
                }
            }
        }
        #endregion

        #region Конструктор класса
        public MainViewModel(SynchronizationContext syncContext)
        {
            TextTitle = "Программное обеспечение на: " + Environment.MachineName;
            Version = "Версия 2.0.2";
            _syncContext = syncContext;
            _defaultIcon = GetIcon("msiexec.exe");
            _isRemoteMode = false;
            _remoteHost = "";
            _isEnableLogin = true;
            _isEnablePass = true;
            _isEnableSelectPC = true;
            _isEnableSearchFiel = true;
            InitializeDataAsync();
        }
        #endregion

        #region События
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public static event MessageEventHandler messageEvent;
        // Событие закрытия приложения
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены что хотите закрыть приложение?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }
        // Сгенерировано событие отображения информации процесса загрузки
        private void updateInfoSearchProcess(object sender, MessageEventArgs e)
        {
            StatusText = e.message;
        }
        #endregion
        
        #region Методы
        // Инициализация данных
        private void InitializeDataAsync()
        {
            IsEnableSelectPC = false;
            IsEnableLogin = false;
            IsEnablePass = false;
            IsEnableSearchFiel = false;
            messageEvent += updateInfoSearchProcess;
            Task.Factory.StartNew(() =>
            {
                string errorMsg = "";
                ObservableCollection<Soft> data = InitListViewData(ref errorMsg);
                if(string.IsNullOrWhiteSpace(errorMsg))
                    _syncContext.Post(OnLoadData, data);
                else
                {
                    _syncContext.Post(OnLoadData, new ObservableCollection<Soft>());
                    _syncContext.Post(ShowErrorMessage, "Ошибка загрузки списка ПО:\r\n" +errorMsg);
                }
            });
        }
        // Загрузка данных
        private void OnLoadData(object state)
        {
            ObservableCollection<Soft> data = (ObservableCollection<Soft>)state;
            data.ToList().ForEach(x => { initImageIcon(ref x); });
            InstalledSoft = data;
            InstalledSoftView.MoveCurrentToFirst();
            OnPropertyChanged("DelButtonIsEnable");
            IsEnableSelectPC = true;
            IsEnableLogin = true;
            IsEnablePass = true;
            IsEnableSearchFiel = true;
            messageEvent -= updateInfoSearchProcess;
            StatusText = "";
        }
        // Инициализация иконки в записи ПО
        private void initImageIcon(ref Soft programm)
        {
            if (!_isRemoteMode)
            {
                if (programm.IconImagePath.IndexOf("default") >= 0)
                {
                    programm.Icon = _defaultIcon;
                }
                else if (programm.IconImagePath.IndexOf(".exe") >= 0)
                {
                    programm.Icon = GetIcon(programm.IconImagePath);
                }
                else if (programm.IconImagePath.IndexOf(".ico") >= 0)
                {
                    programm.Icon = new BitmapImage(new Uri(programm.IconImagePath, UriKind.RelativeOrAbsolute));
                }
                else
                {
                    programm.Icon = _defaultIcon;
                }
            }
            else
                programm.Icon = _defaultIcon;
        }
        // Сортировка
        public void Sort(object parameter)
        {
            string column = parameter as string;
            if (_sortColumn == column)
            {
                _sortDirection = _sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
            else
            {
                _sortColumn = column;
                _sortDirection = ListSortDirection.Ascending;
            }

            _installedSoftView.SortDescriptions.Clear();
            _installedSoftView.SortDescriptions.Add(new SortDescription(_sortColumn, _sortDirection));
        } 
        // Добавление текста в заголовок приложения
        private void addInTextTittle(string text)
        {
            TextTitle += text;
        }
        // Извлечение списка ПО из реестра
        private string ExtractSoftFromRegistry(string rkey)
        {
            RegistryKey printKey;
            String str = "";
            if (Environment.Is64BitOperatingSystem)
            {
                printKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            else
            {
                printKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            }
            messageEvent(null, new MessageEventArgs("Запрос данных из реестра..."));
            String[] names = printKey.OpenSubKey(rkey).GetSubKeyNames();

            foreach (String s in names)
            {
                messageEvent(null, new MessageEventArgs("Обработка реестра: " + s));
                RegistryKey Key = printKey.OpenSubKey(rkey + "\\" + s);
                if (Key != null)
                {
                    var name = Key.GetValue("DisplayName");
                    if ((Key.GetValue("ParentKeyName") == null) && (name != null))
                    {
                        var uninstallstring = Key.GetValue("UninstallString");
                        if (!String.IsNullOrWhiteSpace((string)uninstallstring))
                        {
                            str += ((string)name).Trim() + ";" + ((string)uninstallstring).Trim() + ";";
                            var icon = Key.GetValue("DisplayIcon");
                            if(icon != null)
                                str += ((string)icon).Trim() + ";";
                            else
                                str += " ;";
                            var ver = Key.GetValue("DisplayVersion");
                            if (ver != null)
                                str += ((string)ver).Trim() + "|";
                            else
                                str += " |";
                        }
                    }
                }
            }
            return str;
        }
        // Извлечение списка ПО из удалённого реестра от имени указанного пользователя
        private string ExtractSoftFromRemoteRegistry(string rkey, string compName, string domain, string user, string pass, ref string errMessage)
        {
            string str = "";
            ConnectionOptions connection = new ConnectionOptions();
            connection.Username = user;
            connection.Password = pass;
            connection.Authority = "ntlmdomain:" + domain;
            ManagementScope scope;
            string localPCname = Environment.MachineName;
            try
            {
                messageEvent(null, new MessageEventArgs("Подключение к удалённому компьютеру..."));
                if (localPCname == compName)
                {
                    scope = new ManagementScope("\\\\.\\root\\CIMV2");
                }
                else
                {
                    scope = new ManagementScope("\\\\" + compName + "\\root\\CIMV2", connection);
                }
                scope.Connect();
                ManagementClass registry = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);
                ManagementBaseObject inParams = registry.GetMethodParameters("EnumKey");
                inParams["hDefKey"] = 0x80000002;//HKEY_LOCAL_MACHINE
                inParams["sSubKeyName"] = rkey;
                messageEvent(null, new MessageEventArgs("Запрос данных из реестра удалённого компьютера..."));
                ManagementBaseObject outParams = registry.InvokeMethod("EnumKey", inParams, null);
                string[] namesU = outParams["sNames"] as string[];
                if (namesU != null)
                {
                    foreach (string s in namesU)
                    {
                        messageEvent(null, new MessageEventArgs("Обработка реестра: " + s));
                        inParams = registry.GetMethodParameters("GetStringValue");
                        inParams["hDefKey"] = 0x80000002;//HKEY_LOCAL_MACHINE
                        inParams["sSubKeyName"] = rkey + @"\" + s;
                        inParams["sValueName"] = "DisplayName";
                        outParams = registry.InvokeMethod("GetStringValue", inParams, null);
                        var name = outParams.Properties["sValue"].Value;

                        inParams["sValueName"] = "ParentKeyName";
                        outParams = registry.InvokeMethod("GetStringValue", inParams, null);
                        var parentKeyName = outParams.Properties["sValue"].Value;
                        if (parentKeyName == null && name != null)
                        {
                            inParams["sValueName"] = "UninstallString";
                            outParams = registry.InvokeMethod("GetStringValue", inParams, null);
                            var uninstallString = outParams.Properties["sValue"].Value;
                            if (uninstallString != null && !string.IsNullOrWhiteSpace(uninstallString.ToString()))
                            {
                                str += ((string)name).Trim() + ";" + ((string)uninstallString).Trim() + ";";

                                inParams["sValueName"] = "DisplayIcon";
                                outParams = registry.InvokeMethod("GetStringValue", inParams, null);
                                var displayIcon = outParams.Properties["sValue"].Value;
                                if (displayIcon != null)
                                    str += ((string)displayIcon).Trim() + ";";
                                else
                                    str += " ;";

                                inParams["sValueName"] = "DisplayVersion";
                                outParams = registry.InvokeMethod("GetStringValue", inParams, null);
                                var displayVersion = outParams.Properties["sValue"].Value;
                                if (displayVersion != null)
                                    str += ((string)displayVersion).Trim() + "|";
                                else
                                    str += " |";
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                errMessage = exp.Message;
            }
            return str;
        }
        // Инициализация списка установленного ПО
        private ObservableCollection<Soft> InitListViewData(ref string errMsg)
        {
            ObservableCollection<Soft> listSoft = new ObservableCollection<Soft>();
            string str = "";
            string prevNameSoft = "";
            string iconImagePath = "";
            string ver = "";
            string errorMessage = "";
            if (!_isRemoteMode)
            {
                TextTitle = "Программное обеспечение на: " + Environment.MachineName;
                if (Environment.Is64BitOperatingSystem)
                {
                    str += ExtractSoftFromRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    str += ExtractSoftFromRegistry(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                }
                else
                {
                    str += ExtractSoftFromRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                }
            }
            else
            {
                string user = "";
                string domain = "";
                string architecture = "";
                if (Login.Contains("\\"))
                {
                    string[] loginArray = Login.Split('\\');
                    if (String.Compare(loginArray[0], ".") == 0)
                    {
                        domain = Environment.MachineName;
                    }
                    else
                    {
                        domain = loginArray[0];
                    }
                    user = loginArray[1];
                }
                else
                {
                    domain = Environment.MachineName;
                    user = Login;
                }
                messageEvent(null, new MessageEventArgs("Получение архитектуры удалённого ПК..."));
                architecture = GetArchitectureRemorePC(_remoteHost, domain, user, Password, ref errorMessage);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    errMsg = "Ошибка получения архитектуры удалённого ПК:\r\n" + errorMessage;
                    return listSoft;
                }
                if (architecture == "64-bit")
                {
                    str += ExtractSoftFromRemoteRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", _remoteHost, domain, user, Password, ref errorMessage);
                    str += ExtractSoftFromRemoteRegistry(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", _remoteHost, domain, user, Password, ref errorMessage);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errMsg = errorMessage;
                        return listSoft;
                    }
                }
                else
                {
                    str += ExtractSoftFromRemoteRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", _remoteHost, domain, user, Password, ref errorMessage);
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errMsg = errorMessage;
                        return listSoft;
                    }
                }
                TextTitle = "Программное обеспечение на: " + _remoteHost;
            }
            
            string[] strArray = str.Split('|');
            Array.Sort(strArray);
            foreach (String soft in strArray)
            {
                string[] strArray2 = soft.Split(';');
                if (strArray2.GetUpperBound(0) > 0)
                {
                    if (String.Compare(prevNameSoft, strArray2[0]) != 0)
                    {
                        try
                        {
                            messageEvent(null, new MessageEventArgs("Обработка списка ПО: " + strArray2[0]));
                            if (!string.IsNullOrWhiteSpace(strArray2[2]))
                            {
                                if (strArray2[2].EndsWith(".exe") && strArray2[2].IndexOf("msiexec.exe") < 0)
                                    iconImagePath = "default";
                                else if (strArray2[2].IndexOf(".exe") >= 0 && strArray2[2].IndexOf("msiexec.exe") < 0)
                                {
                                    iconImagePath = strArray2[2].Substring(0, strArray2[2].IndexOf(".exe") + 4).Trim();
                                }
                                else if (strArray2[2].IndexOf(".ico") >= 0)
                                {
                                    iconImagePath = strArray2[2].Substring(0, strArray2[2].IndexOf(".ico") + 4).Trim();
                                }
                                else
                                {
                                    iconImagePath = "default";
                                }
                            }
                            else
                            {
                                iconImagePath = "default";
                                //ico = new BitmapImage(new Uri(@"/Uninstaller;component/Resources/DefaultSoftIcon.ico", UriKind.RelativeOrAbsolute));
                            }
                            if (!string.IsNullOrWhiteSpace(strArray2[3]))
                            {
                                ver = strArray2[3].Trim();
                            }
                        }
                        catch (Exception)
                        {
                            iconImagePath = "default";
                        }
                        listSoft.Add(new Soft { IconImagePath = iconImagePath, Name = strArray2[0], UninstallString = strArray2[1], Version = ver });
                        prevNameSoft = strArray2[0];
                    }
                }
            }
            return listSoft;
        }
        // Фильтр установленного ПО
        private void installedSoft_Filter(object sender, FilterEventArgs e)

        {
            if (string.IsNullOrEmpty(FilterText))
            { 
                e.Accepted = true;
                return;
            }

            Soft po = e.Item as Soft;
            if (po != null) 
            {
                if (po.Name.ToUpper().Contains(FilterText.ToUpper()))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }
        // Изменен выбранный элемент списка установленного ПО
        private void ListViewSelectionChanged(object sender, EventArgs e)
        {
            if (_installedSoftView != null && _installedSoftView.View != null && _installedSoftView.View.CurrentItem == null)
            {
                if (InstalledSoft.Count > 0)
                    SelectedListItem = InstalledSoft.First();
                if (InstalledSoftView.Count > 0)
                {
                    InstalledSoftView.MoveCurrentToFirst();
                    OnPropertyChanged("DelButtonIsEnable");
                }
            }
        }
        // Запуск процесса удаления выделенного ПО
        private void RunDelSoft()
        {
            string command;
            string arguments;
            if (String.IsNullOrWhiteSpace(Login) || String.IsNullOrEmpty(Password)) 
            {
                MessageBox.Show("Не указанн логин или пароль учетной записи Администратора!!!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
                return; 
            }
            Task.Factory.StartNew(() =>
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                // Имя пользователя и домен
                if (Login.Contains("\\"))
                {
                    string[] strArray = Login.Split('\\');
                    if (String.Compare(strArray[0], ".") == 0)
                    {
                        p.StartInfo.Domain = Environment.MachineName;
                    }
                    else
                    {
                        p.StartInfo.Domain = strArray[0];
                    }
                    p.StartInfo.UserName = strArray[1];
                }
                else
                {
                    p.StartInfo.Domain = "";
                    p.StartInfo.UserName = Login;
                }
                // Команда запуска и аргументы
                try
                {
                    command = SelectedListItem.UninstallString.Substring(0, SelectedListItem.UninstallString.IndexOf(".exe") + 5).Trim();
                }
                catch (ArgumentOutOfRangeException)
                {
                    command = SelectedListItem.UninstallString.Substring(0, SelectedListItem.UninstallString.IndexOf(".exe") + 4).Trim();
                }
                arguments = SelectedListItem.UninstallString.Substring(command.Length).Trim();
                if (!command.StartsWith("\""))
                    command = "\"" + command + "\"";
                p.StartInfo.FileName = command;
                p.StartInfo.Arguments = arguments;
                // Шифруем пароль
                System.Security.SecureString encPassword = new System.Security.SecureString();
                foreach (System.Char c in Password)
                {
                    encPassword.AppendChar(c);
                }
                p.StartInfo.Password = encPassword;
                p.StartInfo.UseShellExecute = false;
                try
                {
                    p.Start();
                }
                catch (Win32Exception e)
                {
                    if (e.NativeErrorCode == 1783 || e.NativeErrorCode == 1326)
                    {
                        _syncContext.Post(ShowErrorMessage, "Не правильно указан логин или пароль администратора!!!");
                        return;
                    }
                    else
                    {
                        _syncContext.Post(ShowErrorMessage, e.ToString());
                        return;
                    }
                }
                catch (Exception e)
                {
                    _syncContext.Post(ShowErrorMessage, e.ToString());
                    return;
                }
                p.WaitForExit();
                _syncContext.Post(UpdateListInstalledSoft, new Object());
            });
        }
        // Обновление списка установленного ПО
        private void UpdateListInstalledSoft()
        {
            InstalledSoft = null;
            InitializeDataAsync();
        }
        private void UpdateListInstalledSoft(object obj)
        {
            InstalledSoft = null;
            InitializeDataAsync();
        }
        // Извлечение иконки из указанного файла
        public static ImageSource GetIcon(string fileName)
        {
            string file;
            Icon icon = null;
            try
            {
                if (string.Compare(fileName, "msiexec.exe") == 0 || string.IsNullOrWhiteSpace(fileName))
                    file = @"C:\Windows\System32\msiexec.exe";
                else
                    file = fileName;
                if (File.Exists(file))
                    icon = Icon.ExtractAssociatedIcon(file);
                else
                    icon = Icon.ExtractAssociatedIcon(@"C:\Windows\System32\msiexec.exe");
            }
            catch 
            {
                icon = Icon.ExtractAssociatedIcon(@"C:\Windows\System32\msiexec.exe");
            }
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        new Int32Rect(0,0,icon.Width, icon.Height),
                        BitmapSizeOptions.FromEmptyOptions());
        }
        // Подключение к удалённому ПК и загрузка списка установленного на нём ПО
        private void ConnectToRemPC()
        {
            if (_isRemoteMode)
            {
                if (String.IsNullOrWhiteSpace(Login) || String.IsNullOrEmpty(Password))
                {
                    MessageBox.Show("Не указанн логин или пароль учетной записи Администратора!!!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (String.IsNullOrWhiteSpace(RemoteHost))
                {
                    MessageBox.Show("Не указано имя удалённого компьютера!!!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                OnPropertyChanged("DelButtonIsEnable");
                // Блокировка интерфейса
                Task.Factory.StartNew(() =>
                {
                    string ipAddress = "";
                    StatusText = "Получение IP адреса";
                    #region Получение IP адреса
                    IPAddress ip = new IPAddress(0);
                    try
                    {
                        foreach (IPAddress currrentIPAddress in Dns.GetHostAddresses(_remoteHost))
                        {
                            if (currrentIPAddress.AddressFamily.ToString() == System.Net.Sockets.AddressFamily.InterNetwork.ToString())
                            {
                                ip = currrentIPAddress;
                                ipAddress = ip.ToString();
                                break;
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        _syncContext.Post(ShowErrorMessage, "Ошибка получения IP адреса удалённого компьютера:\r\n" + exp.Message);
                        return;
                    }
                    #endregion
                    StatusText = "Проверка состояния удалённого ПК";
                    #region Проверка состояния
                    if (!PingHost(ip.ToString()))
                    {
                        _syncContext.Post(ShowErrorMessage, "Удалённый компьютер не в сети!!!");
                        return;
                    }
                    #endregion

                    _syncContext.Post(UpdateListInstalledSoft, new Object());
                });
            }
            else
            {
                UpdateListInstalledSoft();
            }
        }
        // Показать сообщение об ошибке
        private void ShowErrorMessage(object message)
        {
            MessageBox.Show((string)message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        // Получить архитектуру удалённого компьютера
        private string GetArchitectureRemorePC(string compName, string domain, string user, string pass, ref string errMessage)
        {
            string architecture = "";
            ConnectionOptions connection = new ConnectionOptions();
            connection.Username = user;
            connection.Password = pass;
            connection.Authority = "ntlmdomain:" + domain;
            ManagementScope scope;
            string localPCname = Environment.MachineName;
            try
            {
                if (localPCname == compName)
                {
                    scope = new ManagementScope("\\\\.\\root\\CIMV2");
                }
                else
                {
                    scope = new ManagementScope("\\\\" + compName + "\\root\\CIMV2", connection);
                }
                scope.Connect();
                ObjectQuery query = new ObjectQuery("SELECT OSArchitecture FROM Win32_OperatingSystem ");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObject queryObj = searcher.Get().OfType<ManagementObject>().FirstOrDefault();
                architecture = queryObj["OSArchitecture"].ToString();
            }
            catch (Exception exp)
            {
                errMessage = exp.Message;
            }
            return architecture;
        }
        // Метод проверки доступности PC путём пингования
        private bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = new Ping();
            try
            {
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                return pingable;
            }
            return pingable;
        }
        #endregion

        #region Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
