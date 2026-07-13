using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

class Program
{
    public class Contact
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email {  get; set; }

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
                Console.WriteLine("AppData folder was created");
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

    public class ContactService
    {
        private readonly JsonFileRepository _repository; 

        public ContactService(JsonFileRepository repository) {  _repository = repository; }

        public List<Contact> GetAll()
        {
            return _repository.AllContacts();
        }
        public void AddContact(Contact newcontact)
        {
            var allContactsList = _repository.AllContacts();
            allContactsList.Add(newcontact);
            _repository.SaveContacts(allContactsList);
        }

        public List<Contact> SearchContact(string searching_text)
        {
            var allContactsList = _repository.AllContacts();
            return allContactsList.Where(c => c.FullName.Contains(searching_text) || c.PhoneNumber.Contains(searching_text) || c.Email.Contains(searching_text)).ToList();
        }

        public bool EditContact(string searchingcontact, string editversion_name, string editversion_number, string editversion_email)
        {
            var allContactsList = _repository.AllContacts();
            var neededContact = allContactsList.FirstOrDefault(c => c.FullName == searchingcontact || c.PhoneNumber == searchingcontact || c.Email == searchingcontact);

            if (neededContact == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(editversion_name)) neededContact.FullName = editversion_name;
            if (!string.IsNullOrWhiteSpace(editversion_number)) neededContact.PhoneNumber = editversion_number;
            if (!string.IsNullOrWhiteSpace(editversion_email)) neededContact.Email = editversion_email;

            _repository.SaveContacts(allContactsList);
            return true;
        }

        public bool DeleteContact(string searchingcontact)
        {
            var allContactsList = _repository.AllContacts();
            var matches = allContactsList.Where(c => c.FullName == searchingcontact || c.PhoneNumber == searchingcontact || c.Email == searchingcontact).ToList();

            if (matches.Count() == 0) return false;
            if (matches.Count() > 1) return false;

            var neededContact = matches.First();

            allContactsList.Remove(neededContact);

            _repository.SaveContacts(allContactsList);

            return true;
        }

        public bool IsPhoneInUse(string phone)
        {
            return _repository.AllContacts().Any(c => c.PhoneNumber == phone);
        }
    }

    static void Main()
    {
        string filepath = @"C:\Users\User\Desktop\AppData\contacts.json";
        JsonFileRepository repository = new JsonFileRepository(filepath);
        ContactService service = new ContactService(repository);

        bool t = true;
        while (t)
        {
            Console.WriteLine("   Menu\n 1.Add contact\n " +
            "2.View all contacts\n 3.Search contact\n " +
            "4.Edit contact\n 5.Delete contact\n " +
            "6.Export contacts to TXT\n 0.Exit");

            int operation;
            Console.Write("Write the number of your operation: ");
            while (!int.TryParse(Console.ReadLine(), out operation))
            {
                Console.Write("Write the number from 0 to 6: ");
            }

            switch (operation)
            {
                case 0:
                    t = false;
                    break;
                case 1:
                    Console.Write("Write your full name: ");
                    string fullname = Console.ReadLine();

                    string phonenumber = "";
                    Console.Write("Write your phone number: ");
                    bool numberchecking = true;
                    while (numberchecking)
                    {
                        phonenumber = Console.ReadLine();

                        if (service.IsPhoneInUse(phonenumber))
                            Console.Write("This phone number is already in use. Try another number: ");
                        else
                            numberchecking = false;
                    }

                    string email = "";
                    Console.Write("Write  your email: ");
                    Boolean emailchecking = true;
                    while (emailchecking)
                    {
                        email = Console.ReadLine();

                        if (!email.Contains("@"))
                        {
                            Console.Write("Your email must contain '@': ");
                        }
                        else
                            emailchecking = false;
                    }

                    Contact newcontact = new Contact
                    {
                        FullName = fullname,
                        PhoneNumber = phonenumber,
                        Email = email
                    };

                    service.AddContact(newcontact);
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

                    string editversion_name = null;
                    string editversion_number = null;
                    string editversion_email = null;

                    Console.Write("If you want to change name click Y: ");
                    string answer1 = Console.ReadLine();
                    if(answer1 == "Y" || answer1 == "y")
                    {
                        Console.Write("New Full Name: ");
                        editversion_name = Console.ReadLine();
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
                            
                            if (service.IsPhoneInUse(editversion_number))
                                Console.Write("This phone number is already in use. Try another number: ");
                            else
                                number_checking = false;
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

                            if (!editversion_email.Contains("@"))
                            {
                                Console.Write("Your email must contain '@': ");
                            }
                            else
                                email_checking = false;
                        }
                    }
                    bool editSuccessful = service.EditContact(searchingcontact, editversion_name, editversion_number, editversion_email);

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