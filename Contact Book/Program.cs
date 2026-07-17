using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ClosedXML.Excel;


namespace ContactBookApp
{
    public class Contact
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email {  get; set; }

    }

    public class ContactData
    {
        public string Action {  get; set; }
        public Contact OriginalContact { get; set; } = null;
        public Contact NewContact { get; set; } = null;

        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime TimeDetail { get; set; }

        public string NormalTimeDetail
        {
            get => TimeDetail.ToString("yyyy-MM-dd HH:mm:ss");
            set => TimeDetail = DateTime.Parse(value);
        }
    }

    public class ExcelFileRepository
    {
        private readonly string _excelfilepath;

        public ExcelFileRepository(string excelfilepath)
        {
            _excelfilepath = excelfilepath;

            string folder = Path.GetDirectoryName(_excelfilepath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) 
                Directory.CreateDirectory(folder);

            if (!File.Exists(_excelfilepath))
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Contacts");
                    ws.Cell(1, 1).Value = "Full Name";
                    ws.Cell(1, 2).Value = "Phone Number";
                    ws.Cell(1, 3).Value = "Email";

                    ws.Row(1).Style.Font.Bold = true;
                    workbook.SaveAs(_excelfilepath);
                }
            }

        }

        public List<Contact> AllContacts()
        {
            var contactsList = new List<Contact>();

            using (var workbook = new XLWorkbook(_excelfilepath)) 
            {
                var ws = workbook.Worksheet("Contacts");
                var rows = ws.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    contactsList.Add(new Contact
                    {
                        FullName = row.Cell(1).Value.ToString().Trim(),
                        PhoneNumber = row.Cell(2).Value.ToString().Trim(),
                        Email = row.Cell(3).Value.ToString().Trim()
                    });
                }
            }

            return contactsList;
        }

        public void SaveContacts(List<Contact> contacts)
        {
            using (var workbook = new XLWorkbook(_excelfilepath))
            {
                var ws = workbook.Worksheet("Contacts");

                var rowsToDelete = ws.RowsUsed().Skip(1).ToList();
                foreach (var row in rowsToDelete)
                {
                    row.Delete();
                }

                int currentRow = 2;
                foreach (var contact in contacts)
                {
                    ws.Cell(currentRow, 1).Value = contact.FullName;
                    ws.Cell(currentRow, 2).Value = contact.PhoneNumber;
                    ws.Cell(currentRow, 3).Value = contact.Email;
                    currentRow++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(_excelfilepath);
            }
        }
    }

    public class JsonFileRepository
    {
        private readonly string _filepath;

        public JsonFileRepository(string filepath)
        {
            _filepath = filepath;

            string folderpath = Path.GetDirectoryName(_filepath);
            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
                Console.WriteLine("Contacts folder was created");
            }

            if (!File.Exists(_filepath))
            {
                File.WriteAllText(_filepath, "[]");
                Console.WriteLine("Contacts file was created");
            }
        }

        public List<Contact> AllContacts()
        {
            string ourJson = File.ReadAllText(_filepath);
            List<Contact> allContactsList = new List<Contact>();

            if (string.IsNullOrWhiteSpace(ourJson)) return allContactsList;

            allContactsList = JsonSerializer.Deserialize<List<Contact>>(ourJson);

            return allContactsList;

        }

        public void SaveContacts(List<Contact> contacts)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(contacts, options);

            File.WriteAllText(_filepath, updatedJson);
        }
    }

    public class ContactDataRepository
    {
        private readonly string _filepath;

        public ContactDataRepository(string filepath)
        {
            _filepath = filepath;

            string folderpath = Path.GetDirectoryName(_filepath);
            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
                Console.WriteLine("History folder was created");
            }

            if (!File.Exists(_filepath))
            {
                File.WriteAllText(_filepath, "[]");
                Console.WriteLine("History file was created");
            }
        }

        public List<ContactData> AllHistory()
        {
            string ourJson = File.ReadAllText(_filepath);
            if (string.IsNullOrWhiteSpace(ourJson)) return new List<ContactData>();

            return JsonSerializer.Deserialize<List<ContactData>>(ourJson) ?? new List<ContactData>();
        }

        public void AppendHistory(ContactData newData)
        {
            var currentHistory = AllHistory();

            currentHistory.Add(newData);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(currentHistory, options);

            File.WriteAllText(_filepath, updatedJson);
        }
    }

    public class ContactService
    {
        private readonly ExcelFileRepository _excelRepository;
        private readonly JsonFileRepository _repository; 
        private readonly ContactDataRepository _fileRepository;

        public ContactService(ExcelFileRepository excelRepository, JsonFileRepository repository, ContactDataRepository fileRepository) 
        { 
            _excelRepository = excelRepository;  
            _repository = repository; 
            _fileRepository = fileRepository;
        }

        public List<Contact> GetAll()
        {
            return _excelRepository.AllContacts();
        }
        public void AddContact(Contact newcontact, ContactData newContactData)
        {
            var allContactsList = _repository.AllContacts();
            var excel_allContactsList = _excelRepository.AllContacts();
            var allContactsData = _fileRepository.AllHistory();

            excel_allContactsList.Add(newcontact);
            allContactsList.Add(newcontact);
            allContactsData.Add(newContactData);

            _excelRepository.SaveContacts(allContactsList);
            _repository.SaveContacts(allContactsList);
            _fileRepository.AppendHistory(newContactData);
        }

        public List<Contact> SearchContact(string searching_text)
        {
            var allContactsList = _excelRepository.AllContacts();
            return allContactsList.Where(c => c.FullName.Contains(searching_text) || c.PhoneNumber.Contains(searching_text) || c.Email.Contains(searching_text)).ToList();
        }

        public bool EditContact(Contact needed_contact, string editversion_name, string editversion_number, string editversion_email)
        {
            if (needed_contact == null) return false;

            var allContactsList = _excelRepository.AllContacts();

            var contact_to_edit = allContactsList.FirstOrDefault(c => c.PhoneNumber == needed_contact.PhoneNumber || c.Email == needed_contact.Email);

            if (contact_to_edit == null) return false;

            Contact originalContact = new Contact
            {
                FullName = contact_to_edit.FullName,
                PhoneNumber = contact_to_edit.PhoneNumber,
                Email = contact_to_edit.Email
            };

            if (!string.IsNullOrWhiteSpace(editversion_name)) contact_to_edit.FullName = editversion_name;
            if (!string.IsNullOrWhiteSpace(editversion_number)) contact_to_edit.PhoneNumber = editversion_number;
            if (!string.IsNullOrWhiteSpace(editversion_email)) contact_to_edit.Email = editversion_email;

            ContactData contactData = new ContactData
            {
                Action = "Edit",
                OriginalContact = originalContact,
                NewContact = contact_to_edit,
                TimeDetail = DateTime.Now,
            };

            _fileRepository.AppendHistory(contactData);
            _excelRepository.SaveContacts(allContactsList);
            return true;
        }

        public bool DeleteContact(string searchingcontact)
        {
            var allContactsList = _excelRepository.AllContacts();
            var matches = allContactsList.Where(c => c.FullName == searchingcontact || c.PhoneNumber == searchingcontact || c.Email == searchingcontact).ToList();

            if (matches.Count() == 0) return false;
            if (matches.Count() > 1) return false;

            var neededContact = matches.First();

            ContactData contactData = new ContactData
            {
                Action = "Delete",
                OriginalContact = neededContact,
                TimeDetail= DateTime.Now
            };

            allContactsList.Remove(neededContact);

            _fileRepository.AppendHistory(contactData);
            _excelRepository.SaveContacts(allContactsList);

            return true;
        }

        public bool IsPhoneInUse(string phone)
        {
            return _excelRepository.AllContacts().Any(c => c.PhoneNumber == phone);
        }

        public bool PhoneValidation(string phone)
        {
            if (!string.IsNullOrWhiteSpace(phone) && phone.All(char.IsDigit))
                return true;
            else
                return false;
        }

        public bool EmailValidation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            string model = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            return Regex.IsMatch(email, model, RegexOptions.IgnoreCase);
        }
    }

    class Program
    {
        static void Main()
        {
            string filepath = @"C:\Users\User\Desktop\AppData\contacts.json";
            string excel_filepath = @"C:\Users\User\Desktop\AppData\contactsForUser.xlsx";
            string data_filepath = @"C:\Users\User\Desktop\AppData\contactshistory.json";
            JsonFileRepository repository = new JsonFileRepository(filepath);
            ExcelFileRepository excelRepository = new ExcelFileRepository(excel_filepath);
            ContactDataRepository fileRepository = new ContactDataRepository(data_filepath);
            ContactService service = new ContactService(excelRepository, repository, fileRepository);

            bool t = true;
            while (t)
            {
                Console.WriteLine("-----Menu-----\n 1.Add contact\n " +
                "2.View all contacts\n 3.Search contact\n " +
                "4.Edit contact\n 5.Delete contact\n " +
                "0.Exit");

                int operation;
                Console.Write("Write the number of your operation: ");
                while (!int.TryParse(Console.ReadLine(), out operation))
                {
                    Console.Write("Write the number from 0 to 5: ");
                }

                switch (operation)
                {
                    case 0:
                        t = false;
                        break;
                    case 1:
                        string fullname = "";
                        Console.Write("Write your full name: ");
                        bool namechecking = true;

                        while (namechecking)
                        {
                            fullname = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(fullname))
                                namechecking = false;
                            else
                                Console.Write("Name can not be empty or null: ");
                        }

                        string phonenumber = "";
                        Console.Write("Write your phone number: ");
                        bool numberchecking = true;
                        while (numberchecking)
                        {
                            phonenumber = Console.ReadLine();

                            if (service.PhoneValidation(phonenumber))
                            {
                                if (service.IsPhoneInUse(phonenumber))
                                    Console.Write("This phone number is already in use. Try another number: ");
                                else
                                    numberchecking = false;
                            }
                            else
                                Console.Write("Wrong input for phone number. Try again: ");
                        }

                        string email = "";
                        Console.Write("Write  your email: ");
                        Boolean emailchecking = true;
                        while (emailchecking)
                        {
                            email = Console.ReadLine();

                            if (service.EmailValidation(email))
                                emailchecking = false;
                            else
                                Console.Write("Wrong input for email. Try again: ");
                        }

                        Contact newcontact = new Contact
                        {
                            FullName = fullname,
                            PhoneNumber = phonenumber,
                            Email = email
                        };

                        ContactData contactdata = new ContactData
                        {
                            Action = "Add",
                            NewContact = newcontact,
                            TimeDetail = DateTime.Now
                        };

                        service.AddContact(newcontact, contactdata);
                        Console.WriteLine("The contact saved.");
                        Console.WriteLine("------------------------------");
                        break;
                    case 2:

                        int degree = 1;
                        foreach (var contact in service.GetAll())
                        {
                            Console.WriteLine($"{degree}. Full Name: {contact.FullName} | Phone Number: {contact.PhoneNumber} | Email: {contact.Email}");
                            degree++;
                        }
                        Console.WriteLine("------------------------------");
                        break;
                    case 3:
                        Console.Write("Search: ");
                        string searching = Console.ReadLine();
                        var searchresult = service.SearchContact(searching);

                        if (!searchresult.Any())
                        {
                            Console.WriteLine("Contact not found.");
                        }
                        else
                        {
                            int rank = 1;
                            foreach (var contact in searchresult)
                            {
                                Console.WriteLine($"{rank}. Full Name: {contact.FullName} | Phone: {contact.PhoneNumber} | Email: {contact.Email}");
                                rank++;
                            }
                        }
                        Console.WriteLine("------------------------------");
                        break;
                    case 4:
                        Console.Write("Which contact you want to edit? (Enter fullname/phonenumber/email.): ");
                        string searchingcontact = Console.ReadLine();
                        var result = service.SearchContact(searchingcontact);

                        if (!result.Any())
                        {
                            Console.WriteLine("Contact not found.");
                            break;
                        }
                        else
                        {
                            int rank = 1;
                            foreach (var contact in result)
                            {
                                Console.WriteLine($"{rank}. Full Name: {contact.FullName} | Phone: {contact.PhoneNumber} | Email: {contact.Email}");
                                rank++;
                            }
                        }

                        int correct_contact;
                        Console.Write($"Choose the contact's number (Between 1 to {result.Count}): ");
                        while (!int.TryParse(Console.ReadLine(), out correct_contact))
                        {
                            Console.Write("It must be an integer: ");
                        }

                        correct_contact--;
                        var needed_contact = result[correct_contact];

                        string editversion_name = null;
                        string editversion_number = null;
                        string editversion_email = null;

                        Console.Write("If you want to change name click Y: ");
                        string answer1 = Console.ReadLine();
                        if (answer1 == "Y" || answer1 == "y")
                        {
                            Console.Write("New Full Name: ");
                            bool name_checking = true;

                            while (name_checking)
                            {
                                editversion_name = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(editversion_name))
                                    name_checking = false;
                                else
                                    Console.Write("Name can not be empty or null: ");
                            }
                        }

                        Console.Write("If you want to change phone number click Y: ");
                        string answer2 = Console.ReadLine();
                        if (answer2 == "Y" || answer2 == "y")
                        {
                            Console.Write("New Phone Number: ");
                            Boolean number_checking = true;
                            while (number_checking)
                            {
                                editversion_number = Console.ReadLine();

                                if (service.PhoneValidation(editversion_number))
                                {
                                    if (service.IsPhoneInUse(editversion_number))
                                        Console.Write("This phone number is already in use. Try another number: ");
                                    else
                                        number_checking = false;
                                }
                                else
                                    Console.Write("Wrong input for phone number. Try again: ");
                            }
                        }

                        Console.Write("If you want to change email click Y: ");
                        string answer3 = Console.ReadLine();
                        if (answer3 == "Y" || answer3 == "y")
                        {
                            Console.Write("New Email: ");
                            Boolean email_checking = true;
                            while (email_checking)
                            {
                                editversion_email = Console.ReadLine();

                                if (service.EmailValidation(editversion_email))
                                    email_checking = false;
                                else
                                    Console.Write("Wrong input for email. Try again: ");
                            }
                        }



                        bool editSuccessful = service.EditContact(needed_contact, editversion_name, editversion_number, editversion_email);

                        if (editSuccessful)
                            Console.WriteLine("Contact updated and saved successfully!");
                        else
                            Console.WriteLine("Something went wrong updating the contact.");
                        Console.WriteLine("------------------------------");
                        break;
                    case 5:
                        Console.Write("Which contact you want to delete? (Enter fullname/phonenumber/email.): ");
                        string searching_contact = Console.ReadLine();
                        bool searching_by_phone = false;
                        if (searching_contact.All(char.IsDigit)) searching_by_phone = true;

                        if (service.DeleteContact(searching_contact))
                            Console.WriteLine("Contact deleted.");
                        else
                            if (searching_by_phone) Console.WriteLine("Contact not found.");
                            else Console.WriteLine("Something went wrong. Try with phone number.");
                        Console.WriteLine("------------------------------");
                        break;
                }
            }
        }
    }    
}