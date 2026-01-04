using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Gender { get; set; }
    public int YearOfBirth { get; set; }
    public string UserId { get; set; }
    public string PhoneNum { get; set; }
    public int City { get; set; }

    public User()
    {
        Id = 0;
        Username = "";
        Firstname = "";
        Lastname = "";
        Email = "";
        Password = "";
        Gender = 1;
        YearOfBirth = 1900;
        UserId = "000000000";
        PhoneNum = "000-0000000";
        City = 7;
    }

    public User(string firstname, string lastname)
    {
        Id = 0;
        Username = "";
        Firstname = firstname;
        Lastname = lastname;
        Email = "";
        Password = "";
        Gender = 1;
        YearOfBirth = 1900;
        UserId = "000000000";
        PhoneNum = "000-0000000";
        City = 8;
    }
    

    public User(string username, string firstname, string lastname,
        string email, string password, int gender, int yearofbirth, string userid, string phonenum, int city)
    {
        Id = 0;
        Username = username;
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
        Password = password;
        Gender = gender;
        YearOfBirth = yearofbirth;
        UserId = userid;
        PhoneNum = phonenum;
        City = city;
    }

    public void insertintodb()
    {
        UsersService us = new UsersService();
        us.insertIntoDB(
            this.Username,
            this.Firstname,
            this.Lastname,
            this.Email,
            this.Password,
            this.Gender,
            this.YearOfBirth,
            this.UserId,
            this.PhoneNum,
            this.City
        );
    }

}
