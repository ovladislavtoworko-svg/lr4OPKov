using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

public record Parcel(
    string TrackingNumber,
    string Carrier,
    string City,
    string Status,
    double WeightKg,
    int DaysInTransit);

public record Order(
    string OrderId,
    string Customer,
    string TrackingNumber);

class Program
{
    private static List<Parcel> _parcels = new();

    private static readonly List<Order> _orders = new()
    {
        new("ORD-1001", "Іван Петренко", "SP10001"),
        new("ORD-1002", "Олена Коваль", "ED20002"),
        new("ORD-1003", "Андрій Шевченко", "P240003"),
        new("ORD-1004", "Марія Бондар", "SP10004"),
        new("ORD-1005", "Іван Петренко", "ED20005"),
        new("ORD-1006", "Наталія Мельник", "P240006"),
        new("ORD-1007", "Олена Коваль", "SP10007"),
        new("ORD-1008", "Дмитро Ткаченко", "MISSING001"),
        new("ORD-1009", "Андрій Шевченко", "ED20009"),
        new("ORD-1010", "Марія Бондар", "P240010"),
        new("ORD-1011", "Сергій Лисенко", "MISSING002")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    static async Task Main()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== ВІДСТЕЖЕННЯ ПОСИЛОК ==========");
            Console.WriteLine("0. Вихід");
            Console.WriteLine("1. Завантажити дані одночасно");
            Console.WriteLine("2. Завантажити дані послідовно");
            Console.WriteLine("3. Статистика за службами доставки");
            Console.WriteLine("4. Середній термін доставки отриманих посилок");
            Console.WriteLine("5. Пошук затриманих посилок");
            Console.WriteLine("6. Замовлення клієнта");
            Console.WriteLine("7. Зберегти звіт у JSON");

            int choice = ReadInt("Ваш вибір: ", 0, 7);

            switch (choice)
            {
                case 0:
                    running = false;
                    break;
                case 1:
                    await LoadDataAsync(parallel: true);
                    break;
                case 2:
                    await LoadDataAsync(parallel: false);
                    break;
                case 3:
                    ShowCarrierStatistics();
                    break;
                case 4:
                    ShowAverageDeliveryTime();
                    break;
                case 5:
                    ShowDelayedParcels();
                    break;
                case 6:
                    ShowCustomerOrders();
                    break;
                case 7:
                    await SaveReportAsync();
                    break;
            }
        }

        Console.WriteLine("Роботу програми завершено.");
    }

    // Три асинхронні джерела даних — імітація різних служб доставки.
    private static async Task<List<Parcel>> LoadFastPostAsync()
    {
        await Task.Delay(1000);
        return new List<Parcel>
        {
            new("SP10001", "ШвидкоПошта", "Вінниця", "Отримано", 1.2, 2),
            new("SP10004", "ШвидкоПошта", "Київ", "В дорозі", 2.5, 6),
            new("SP10007", "ШвидкоПошта", "Львів", "У відділенні", 0.8, 3),
            new("SP10008", "ШвидкоПошта", "Одеса", "В дорозі", 3.1, 9),
            new("SP10009", "ШвидкоПошта", "Харків", "Отримано", 1.7, 4),
            new("SP10010", "ШвидкоПошта", "Вінниця", "У відділенні", 4.0, 5),
            new("SP10011", "ШвидкоПошта", "Дніпро", "В дорозі", 0.6, 12),
            new("SP10012", "ШвидкоПошта", "Черкаси", "Отримано", 2.2, 3)
        };
    }

    private static async Task<List<Parcel>> LoadExpressAsync()
    {
        await Task.Delay(1500);
        return new List<Parcel>
        {
            new("ED20002", "ЕкспресДоставка", "Житомир", "Отримано", 1.5, 2),
            new("ED20005", "ЕкспресДоставка", "Вінниця", "В дорозі", 2.0, 8),
            new("ED20009", "ЕкспресДоставка", "Київ", "У відділенні", 0.9, 4),
            new("ED20010", "ЕкспресДоставка", "Одеса", "Отримано", 5.3, 5),
            new("ED20011", "ЕкспресДоставка", "Львів", "В дорозі", 1.1, 11),
            new("ED20012", "ЕкспресДоставка", "Харків", "У відділенні", 2.8, 6),
            new("ED20013", "ЕкспресДоставка", "Полтава", "Отримано", 0.7, 3),
            new("ED20014", "ЕкспресДоставка", "Вінниця", "В дорозі", 3.6, 14)
        };
    }

    private static async Task<List<Parcel>> LoadParcel24Async()
    {
        await Task.Delay(2000);
        return new List<Parcel>
        {
            new("P240003", "Посилка24", "Одеса", "Отримано", 2.4, 4),
            new("P240006", "Посилка24", "Київ", "В дорозі", 1.9, 7),
            new("P240010", "Посилка24", "Вінниця", "У відділенні", 4.2, 5),
            new("P240015", "Посилка24", "Львів", "Отримано", 0.5, 2),
            new("P240016", "Посилка24", "Дніпро", "В дорозі", 6.0, 13),
            new("P240017", "Посилка24", "Чернігів", "У відділенні", 1.3, 3),
            new("P240018", "Посилка24", "Харків", "Отримано", 2.7, 6),
            new("P240019", "Посилка24", "Житомир", "В дорозі", 0.4, 10)
        };
    }

    private static async Task LoadDataAsync(bool parallel)
    {
        Console.WriteLine("\nЗавантаження даних служб доставки...");
        var stopwatch = Stopwatch.StartNew();

        List<Parcel>[] parts;

        if (parallel)
        {
            parts = await Task.WhenAll(
                LoadFastPostAsync(),
                LoadExpressAsync(),
                LoadParcel24Async());
        }
        else
        {
            parts = new[]
            {
                await LoadFastPostAsync(),
                await LoadExpressAsync(),
                await LoadParcel24Async()
            };
        }

        stopwatch.Stop();
        _parcels = parts.SelectMany(part => part).ToList();

        Console.WriteLine($"Завантажено посилок: {_parcels.Count}");
        Console.WriteLine($"Режим: {(parallel ? "одночасний" : "послідовний")}");
        Console.WriteLine($"Час виконання: {stopwatch.ElapsedMilliseconds} мс");
    }

    private static bool HasData()
    {
        if (_parcels.Count > 0)
            return true;

        Console.WriteLine("Дані відсутні. Спочатку виконайте завантаження (пункт 1 або 2).");
        return false;
    }

    // LINQ 1: групування та агрегування за службами доставки.
    private static void ShowCarrierStatistics()
    {
        if (!HasData()) return;

        var statistics = _parcels
            .GroupBy(p => p.Carrier)
            .Select(g => new
            {
                Carrier = g.Key,
                InTransit = g.Count(p => p.Status == "В дорозі"),
                AtOffice = g.Count(p => p.Status == "У відділенні"),
                Delivered = g.Count(p => p.Status == "Отримано"),
                TotalWeight = g.Sum(p => p.WeightKg)
            })
            .OrderBy(s => s.Carrier);

        Console.WriteLine("\nСТАТИСТИКА ЗА СЛУЖБАМИ");
        foreach (var s in statistics)
        {
            Console.WriteLine($"{s.Carrier}: в дорозі — {s.InTransit}, " +
                $"у відділенні — {s.AtOffice}, отримано — {s.Delivered}, " +
                $"сумарна вага — {s.TotalWeight:F1} кг");
        }
    }

    // LINQ 2: query-синтаксис, фільтрація, групування, середнє та сортування.
    private static void ShowAverageDeliveryTime()
    {
        if (!HasData()) return;

        var averages =
            from p in _parcels
            where p.Status == "Отримано"
            group p by p.Carrier into carrierGroup
            let averageDays = carrierGroup.Average(p => p.DaysInTransit)
            orderby averageDays, carrierGroup.Key
            select new
            {
                Carrier = carrierGroup.Key,
                AverageDays = averageDays,
                Count = carrierGroup.Count()
            };

        Console.WriteLine("\nСЕРЕДНІЙ ТЕРМІН ДОСТАВКИ ОТРИМАНИХ ПОСИЛОК");
        foreach (var item in averages)
            Console.WriteLine($"{item.Carrier}: {item.AverageDays:F2} дн. ({item.Count} посилок)");
    }

    // LINQ 3: Where, OrderByDescending і ThenBy.
    private static void ShowDelayedParcels()
    {
        if (!HasData()) return;

        int days = ReadInt("Введіть мінімальну кількість днів затримки (N > 0): ", 1);

        var delayed = _parcels
            .Where(p => p.Status == "В дорозі" && p.DaysInTransit > days)
            .OrderByDescending(p => p.DaysInTransit)
            .ThenBy(p => p.Carrier)
            .Select(p => new
            {
                p.TrackingNumber,
                p.Carrier,
                p.City,
                p.DaysInTransit,
                p.WeightKg
            })
            .ToList();

        Console.WriteLine("\nЗАТРИМАНІ ПОСИЛКИ");
        if (!delayed.Any())
        {
            Console.WriteLine("Посилок за заданою умовою не знайдено.");
            return;
        }

        foreach (var p in delayed)
            Console.WriteLine($"{p.TrackingNumber} | {p.Carrier} | {p.City} | " +
                $"{p.DaysInTransit} дн. | {p.WeightKg:F1} кг");
    }

    // LINQ 4: GroupJoin зберігає замовлення, навіть якщо посилку не знайдено.
    private static void ShowCustomerOrders()
    {
        if (!HasData()) return;

        Console.Write("Введіть ім'я клієнта: ");
        string customer = (Console.ReadLine() ?? "").Trim();

        var customerOrders = _orders
            .Where(o => o.Customer.Equals(customer, StringComparison.OrdinalIgnoreCase))
            .GroupJoin(
                _parcels,
                order => order.TrackingNumber,
                parcel => parcel.TrackingNumber,
                (order, matches) => new
                {
                    order.OrderId,
                    order.Customer,
                    order.TrackingNumber,
                    Parcel = matches.FirstOrDefault()
                })
            .ToList();

        if (!customerOrders.Any())
        {
            Console.WriteLine("Замовлень не знайдено.");
            return;
        }

        Console.WriteLine("\nЗАМОВЛЕННЯ КЛІЄНТА");
        foreach (var order in customerOrders)
        {
            string status = order.Parcel?.Status ?? "Статус невідомий";
            string city = order.Parcel?.City ?? "Місто невідоме";
            string carrier = order.Parcel?.Carrier ?? "Службу не визначено";

            Console.WriteLine($"{order.OrderId} | ТТН: {order.TrackingNumber} | " +
                $"{carrier} | {status} | {city}");
        }
    }

    private static async Task SaveReportAsync()
    {
        if (!HasData()) return;

        var report = _orders
            .GroupJoin(
                _parcels,
                order => order.TrackingNumber,
                parcel => parcel.TrackingNumber,
                (order, matches) => new
                {
                    order.OrderId,
                    order.Customer,
                    order.TrackingNumber,
                    Status = matches.Select(p => p.Status).FirstOrDefault() ?? "Статус невідомий",
                    City = matches.Select(p => p.City).FirstOrDefault() ?? "Місто невідоме",
                    Carrier = matches.Select(p => p.Carrier).FirstOrDefault() ?? "Службу не визначено"
                })
            .ToList();

        string json = JsonSerializer.Serialize(report, JsonOptions);

        try
        {
            await File.WriteAllTextAsync("parcel_report.json", json);
            Console.WriteLine("Звіт успішно збережено у файл parcel_report.json.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Помилка запису файлу: {ex.Message}");
        }
    }

    private static int ReadInt(string prompt, int min, int max = int.MaxValue)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) &&
                value >= min && value <= max)
                return value;

            Console.WriteLine($"Введіть ціле число в межах від {min} до {max}.");
        }
    }
}
