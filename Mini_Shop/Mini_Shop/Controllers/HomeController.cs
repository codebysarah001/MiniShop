using Mini_Shop.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System;
using System.IO;
using System.Web;
using System.Collections.Generic;

namespace Mini_Shop.Controllers
{
    public class HomeController : Controller
    {
        private Mini_ShopEntities1 db = new Mini_ShopEntities1();

        public ActionResult Index()
        {
            var products = db.Products.Include(p => p.Category).ToList();
            return View(products);
        }

        // GET: Contact
        public ActionResult Contact()
        {
            return View();
        }

        // POST: Contact/Submit
        [HttpPost]
        public ActionResult Submit(string name, string email, string subject, string message)
        {
            // Process the form data here (e.g., save it to a database or send an email)

            // Set a success message to be displayed on the Contact page
            ViewBag.Message = "Thank you for contacting us! We will get back to you shortly.";

            return View("Contact"); // Return to the Contact view with the message
        }


        public ActionResult FoodCategory()
        {
            ViewBag.Message = "Food category page.";
            return View();
        }

        public ActionResult ToysCategory()
        {
            ViewBag.Message = "Toys category page.";
            return View();
        }

        public ActionResult AccessoriesCategory()
        {
            ViewBag.Message = "Accessories category page.";
            return View();
        }

        public ActionResult DentalCategory()
        {
            ViewBag.Message = "Dental category page.";
            return View();
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register( User user)
        {
            if (ModelState.IsValid && user.PasswordHash == user.ConfirmPassword)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(user);
        }


        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == password);

            if (user != null)
            {
                Session["Userid"] = user.UserID;
                return RedirectToAction("Index");
            }

            ViewBag.UsernameError = "Invalid username or password.";
            return View();
        }


        public ActionResult Shop()
        {
            return View();
        }

        public ActionResult ReadMore(int id)
        {
            // Check if the session variable is null
            if (Session["Userid"] == null)
            {
                // Redirect to login page
                return RedirectToAction("Login", "Home");
            }
            else
            {
                var product = db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == id);

                if (product == null)
                {
                    return View("Error", new HandleErrorInfo(new Exception("Product not found"), "Home", "ReadMore"));
                }

                return View(product);
            }
        }


        public ActionResult ProfileDisplay()
        {
            if (Session["Userid"] != null)
            {
                int userId = (int)Session["Userid"];
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    return View(user);
                }
            }

            return RedirectToAction("Login");
        }


        public ActionResult ProfileEdit()
        {
            if (Session["Userid"] != null)
            {
                int userId = (int)Session["Userid"];
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    return View(user);
                }
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProfileEdit(User user)
        {
            var id1 = (int)Session["Userid"];
            var existingUser1 = db.Users.Find(id1);

            if (ModelState.IsValid)
            {
               
                if (existingUser1 != null)
                {
                    existingUser1.Username = user.Username;
                    existingUser1.ConfirmPassword = user.ConfirmPassword;
                    existingUser1.Email = user.Email;
                    existingUser1.FullName = user.FullName;
                    existingUser1.Address = user.Address;
                    existingUser1.PhoneNumber = user.PhoneNumber;
                    db.Entry(existingUser1).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("ProfileDisplay");
                }
            }

            return View(user);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId)
        {
            var userId = (int)Session["UserId"];

            // Check if the cart exists for the user, if not create a new one
            var cart = db.Carts.FirstOrDefault(c => c.UserID == userId && c.ProductID == productId);

            if (cart == null)
            {
                // Assuming product details are retrieved from a Products table
                var product = db.Products.Find(productId);
                if (product != null)
                {
                    cart = new Cart
                    {
                        UserID = userId,
                        ProductID = productId,
                        ProductName = product.ProductName,
                        Price = product.Price.ToString(),
                        ImageURL = product.ImageUrl,
                        Quantity = 1 
                    };
                    db.Carts.Add(cart);
                }
            }
            else
            {
                cart.Quantity += 1;
            }

            db.SaveChanges();

            return RedirectToAction("ViewCart");
        }


        public ActionResult ViewCart()
        {
            // Check if the user session is null
            if (Session["UserId"] == null)
            {
                // Redirect to the login page if the session is null
                return RedirectToAction("Login", "Home");
            }

            // Proceed with retrieving the cart items
            var userId = (int)Session["UserId"];
            var cartItems = db.Carts.Where(c => c.UserID == userId).ToList();

            // Pass the cart items to the view
            return View(cartItems);
        }




        [HttpPost]
        public ActionResult RemoveProduct(int productId)
        {
            var cartItem = db.Carts.FirstOrDefault(c => c.ProductID == productId);

            if (cartItem != null)
            {
                db.Carts.Remove(cartItem);
                db.SaveChanges();
            }

            return RedirectToAction("ViewCart");
        }

        public ActionResult Checkout()
        {
            var userId = (int)Session["UserId"];

            // Fetch cart items including product details
            var cartItems = db.Carts
                .Where(c => c.UserID == userId)
                .ToList();

            // Calculate total amount
            var totalAmount = cartItems.Sum(item =>
                (item.Quantity ?? 0) *
                (decimal.TryParse(item.Price, out decimal price) ? price : 0));

            ViewBag.TotalAmount = totalAmount;

            return View(cartItems);
        }



        [HttpPost]
        public ActionResult CompleteCheckout(string address)
        {
            var userId = (int)Session["UserId"];
            var cart = db.Carts.FirstOrDefault(c => c.UserID == userId);

            if (cart == null)
            {
                return RedirectToAction("Index"); // Redirect if no cart found
            }

            var cartItems = db.Carts
                .Where(ci => ci.CartID == cart.CartID)
                .ToList();

            // Save order details and shipping address here
            // Example:
            var totalAmount = cartItems.Sum(item =>
                (item.Quantity ?? 0) *
                (decimal.TryParse(item.Price, out var price) ? price : 0)
            );

            // Create and save order details
            // var order = new Order { UserID = userId, Address = address, TotalAmount = totalAmount };
            // db.Orders.Add(order);
            // db.SaveChanges();

            // Clear the cart after completing the purchase
            db.Carts.RemoveRange(cartItems);
            db.SaveChanges();

            return RedirectToAction("OrderConfirmation"); // Redirect to a confirmation page
        }



        public ActionResult Logout()
        {
            Session["Userid"] = null;
            return RedirectToAction("Index");
        }

    }
}
