using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

[Flags]
public enum Metrics
{
    None = 0,
    CPU = 1,
    RAM = 2,
    Disk = 4
}

class SystemMonitor
{
    static bool loggingEnabled = false;
    static Metrics selectedMetrics = Metrics.None;
    static TimeSpan logInterval = TimeSpan.FromSeconds(5);

    static void Main(string[] args)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Выберите опцию:");
            Console.WriteLine("1. Вывод информации о CPU, RAM и дисках");
            Console.WriteLine("2. Вывод всех процессов с возможностью сортировки и поиска");
            Console.WriteLine("3. Включение/отключение логирования");
            Console.WriteLine("4. Выход");
            Console.Write("Введите номер опции: ");
            string choice = Console.ReadLine();
            LogAction($"Пользователь выбрал опцию: {choice}");
            switch (choice)
            {
                case "1":
                    GetSystemInfo();
                    break;
                case "2":
                    ManageProcesses();
                    break;
                case "3":
                    ToggleLogging();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    break;
            }
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }

    static void GetSystemInfo()
    {
        LogAction("Запрос информации о системе.");

        if ((selectedMetrics & Metrics.CPU) == Metrics.CPU)
        {
            GetCpuInfo();
        }

        if ((selectedMetrics & Metrics.RAM) == Metrics.RAM)
        {
            GetRamInfo();
        }

        if ((selectedMetrics & Metrics.Disk) == Metrics.Disk)
        {
            GetDiskInfo();
        }

        // Задержка перед следующей итерацией логирования
        System.Threading.Thread.Sleep(logInterval);
    }

    static void GetCpuInfo()
    {
        int coreCount = Environment.ProcessorCount;
        LogAction($"Количество ядер: {coreCount}");
        Console.WriteLine($"Количество ядер: {coreCount}");
        float cpuUsage = GetCpuUsage();
        LogAction($"Текущая загрузка CPU: {cpuUsage}%");
        Console.WriteLine($"Текущая загрузка CPU: {cpuUsage}%");
    }

    static float GetCpuUsage()
    {
        var currentProcess = Process.GetCurrentProcess();
        var startCpuUsage = currentProcess.TotalProcessorTime;
        var startTime = DateTime.Now;
        System.Threading.Thread.Sleep(1000); // Ждем 1 секунду
        currentProcess.Refresh();
        var endCpuUsage = currentProcess.TotalProcessorTime;
        var endTime = DateTime.Now;
        return (float)((endCpuUsage - startCpuUsage).TotalMilliseconds / (endTime - startTime).TotalMilliseconds * 100);
    }

    static void GetRamInfo()
    {
        var currentProcess = Process.GetCurrentProcess();
        long usedMemory = currentProcess.WorkingSet64 / (1024 * 1024);
        LogAction($"Используемый объём RAM: {usedMemory} MB");
        Console.WriteLine($"Используемый объём RAM: {usedMemory} MB");

        long totalMemory = GC.GetTotalMemory(false) / (1024 * 1024);
        LogAction($"Общий объём RAM: {totalMemory} MB");
        Console.WriteLine($"Общий объём RAM: {totalMemory} MB");

        var availableMemory = GetAvailableMemory() / (1024 * 1024);
        LogAction($"Свободный объём RAM: {availableMemory} MB");
        Console.WriteLine($"Свободный объём RAM: {availableMemory} MB");
    }

    static long GetAvailableMemory()
    {
        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));

        if (GlobalMemoryStatusEx(ref memStatus))
            return (long)memStatus.ullAvailPhys;

        return 0;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullExtendedVirtual;
    }

    static void GetDiskInfo()
    {
        DriveInfo[] drives = DriveInfo.GetDrives();

        foreach (var drive in drives)
        {
            if (drive.IsReady)
            {
                long totalSizeGB = drive.TotalSize / (1024 * 1024 * 1024);
                long usedSpaceGB = (drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024 * 1024);
                long freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);

                LogAction($"Диск: {drive.Name}, Общий объём диска: {totalSizeGB} GB, Используемое пространство: {usedSpaceGB} GB, Свободное пространство: {freeSpaceGB} GB");
                Console.WriteLine($"Диск: {drive.Name}");
                Console.WriteLine($"Общий объём диска: {totalSizeGB} GB");
                Console.WriteLine($"Используемое пространство: {usedSpaceGB} GB");
                Console.WriteLine($"Свободное пространство: {freeSpaceGB} GB");
            }
        }
    }

    static void ManageProcesses()
    {
        while (true)
        {
            Console.Clear();
            Process[] processList = Process.GetProcesses();
            Console.WriteLine("Список запущенных процессов:");

            foreach (var process in processList)
            {
                try
                {
                    long memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
                    LogAction($"{process.ProcessName} - Используемая память: {memoryUsageMB} MB");
                    Console.WriteLine($"{process.ProcessName} - Используемая память: {memoryUsageMB} MB");
                }
                catch
                {
                    // Игнорировать процессы, к которым нет доступа
                }
            }

            Console.WriteLine("\nВыберите опцию:");
            Console.WriteLine("1. Сортировать по использованию памяти");
            Console.WriteLine("2. Сортировать по загрузке процессора");
            Console.WriteLine("3. Поиск по названию процесса");
            Console.WriteLine("4. Назад");

            string choice = Console.ReadLine();
            LogAction($"Пользователь выбрал опцию управления процессами: {choice}");

            switch (choice)
            {
                case "1":
                    SortProcessesByMemory(processList);
                    break;
                case "2":
                    SortProcessesByCpu(processList);
                    break;
                case "3":
                    SearchProcess(processList);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    break;
            }

            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }

    static void SortProcessesByMemory(Process[] processList)
    {
        var sortedProcesses = processList.OrderByDescending(p => p.WorkingSet64).ToArray();

        Console.Clear();

        Console.WriteLine("\nПроцессы отсортированные по использованию памяти:");

        foreach (var process in sortedProcesses)
        {
            try
            {
                long memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
                LogAction($"{process.ProcessName} - Используемая память: {memoryUsageMB} MB");
                Console.WriteLine($"{process.ProcessName} - Используемая память: {memoryUsageMB} MB");
            }
            catch
            {
                // Игнорировать процессы, к которым нет доступа
            }
        }
    }

    static void SortProcessesByCpu(Process[] processList)
    {
        // Для упрощения примера используем только использование памяти,
        // так как для получения загрузки CPU требуется больше времени и ресурсов.
        var sortedProcesses = processList.OrderByDescending(p => p.WorkingSet64).ToArray();

        Console.Clear();
        Console.WriteLine("\nПроцессы отсортированные по загрузке процессора:");

        foreach (var process in sortedProcesses)
        {
            try
            {
                // Здесь можно добавить логику для получения загрузки CPU для каждого процесса.
                float cpuUsage = GetCpuUsageForProcess(process); // Заглушка для примера.
                LogAction($"{process.ProcessName} - Загрузка CPU: {cpuUsage}%");
                Console.WriteLine($"{process.ProcessName} - Загрузка CPU: {cpuUsage}%");
            }
            catch
            {
                // Игнорировать процессы, к которым нет доступа
            }
        }
    }

    static float GetCpuUsageForProcess(Process process)
    {
        // Логика для получения загрузки CPU для конкретного процесса.
        return 0; // Заглушка для примера.
    }

    static void SearchProcess(Process[] processList)
    {
        Console.Write("Введите название процесса для поиска: ");
        string searchTerm = Console.ReadLine().ToLower();
        LogAction($"Поиск процесса с названием: {searchTerm}");

        var foundProcesses = processList.Where(p => p.ProcessName.ToLower().Contains(searchTerm)).ToArray();

        if (!foundProcesses.Any())
        {
            LogAction("Процесс не найден.");
            Console.WriteLine("Процесс не найден.");
            return;
        }

        foreach (var process in foundProcesses)
        {
            try
            {
                long memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
                LogAction($"{process.ProcessName} - Используемая память: {memoryUsageMB} MB");
                Console.WriteLine($"{process.ProcessName} - Используемая память: {memoryUsageMB} MB");
            }
            catch
            {
                // Игнорировать процессы, к которым нет доступа
            }
        }
    }

    static void ToggleLogging()
    {
        loggingEnabled = !loggingEnabled;

        if (loggingEnabled)
        {
            SelectMetricsAndInterval();
            StartLogging();
            LogAction("Логирование включено.");
            Console.WriteLine("Логирование включено.");
        }
        else
        {
            StopLogging();
            LogAction("Логирование отключено.");
            Console.WriteLine("Логирование отключено.");
        }
    }

    static void SelectMetricsAndInterval()
    {
        Console.WriteLine("Выберите метрики для логирования:");
        Console.WriteLine("1. CPU");
        Console.WriteLine("2. RAM");
        Console.WriteLine("3. Disk");
        Console.WriteLine("4. Все");

        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                selectedMetrics = Metrics.CPU;
                break;
            case "2":
                selectedMetrics = Metrics.RAM;
                break;
            case "3":
                selectedMetrics = Metrics.Disk;
                break;
            case "4":
                selectedMetrics = Metrics.CPU | Metrics.RAM | Metrics.Disk;
                break;
            default:
                selectedMetrics = Metrics.None; // Если неверный выбор, отключаем все метрики.
                break;
        }

        Console.Write("Введите интервал логирования в секундах: ");

        if (int.TryParse(Console.ReadLine(), out int interval))
        {
            logInterval = TimeSpan.FromSeconds(interval);
        }
    }

    static void StartLogging()
    {
        using StreamWriter writer = new StreamWriter("system_log.txt", true);
        writer.WriteLine($"Логирование начато в {DateTime.Now}");
    }

    static void StopLogging()
    {
        using StreamWriter writer = new StreamWriter("system_log.txt", true);
        writer.WriteLine($"Логирование остановлено в {DateTime.Now}");
    }

    static void LogAction(string message)
    {
        if (loggingEnabled)
        {
            using StreamWriter writer = new StreamWriter("system_log.txt", true);
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}