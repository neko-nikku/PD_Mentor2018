using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

/*********************************
**ADD THESE USING STATEMENTS
*********************************/

//these two let us work with our database
using System.Data.SqlClient;
using System.Data;

//this one lets us grab our database connection string from web.config
using System.Configuration;

namespace PDMentor_Project
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    //READ THE MICROSOFT COMMENT BELOW
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    //YES, UNCOMMENT THIS LINE BELOW
    //this tells asp.net it's ok to talk in json if asked to!
    [System.Web.Script.Services.ScriptService]

    public class pdmentorwebservice : System.Web.Services.WebService
    {
        //Now we can start adding web methods
        //don't forget the webmethod decoration
        //and don't forget to enable sessions

        /////////////////////////////////////////////////////////
        //EXAMPLE OF: select query, with returned data
        //				putting a value into a session variable
        /////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public bool LogOn(string uid, string pass)
        {
            //LOGIC: pass the parameters into the database to see if an account
            //with these credentials exist.  If it does, then log them on by
            //storing their unique ID in the session so that other webmethods
            //can retrieve it and confirm that they logged in.  If it doesn't,
            //we need to signal to them that they did not successfully log on.

            //we return this flag to tell them if they logged in or not
            bool success = false;

            //our connection string comes from our web.config file like we talked about earlier
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //here's our query.  A basic select with nothing fancy.  Note the parameters that begin with @
            string sqlSelect = "SELECT id FROM useraccount WHERE username=@idValue and password=@passValue";

            //set up our connection object to be ready to use our connection string
            SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
            //set up our command object to use our connection, and our query
            SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

            //tell our command to replace the @parameters with real values
            //we decode them because they came to us via the web so they were encoded
            //for transmission (funky characters escaped, mostly)
            sqlCommand.Parameters.Add("@idValue", System.Data.SqlDbType.NVarChar);
            sqlCommand.Parameters["@idValue"].Value = HttpUtility.UrlDecode(uid);
            sqlCommand.Parameters.Add("@passValue", System.Data.SqlDbType.NVarChar);
            sqlCommand.Parameters["@passValue"].Value = HttpUtility.UrlDecode(pass);

            //a data adapter acts like a bridge between our command object and 
            //the data we are trying to get back and put in a table object
            SqlDataAdapter sqlDa = new SqlDataAdapter(sqlCommand);
            //here's the table we want to fill with the results from our query
            DataTable sqlDt = new DataTable();
            //here we go filling it!
            sqlDa.Fill(sqlDt);
            //check to see if any rows were returned.  If they were, it means it's 
            //a legit account
            if (sqlDt.Rows.Count > 0)
            {
                //store the id in the session so other web methods can access it later
                Session["id"] = sqlDt.Rows[0]["id"];
                //flip our flag to true so we return a value that lets them know they're logged in
                success = true;
            }
            //return the result!
            return success;
        }


        /////////////////////////////////////////////////////////
        //EXAMPLE OF: clearing out all session variables
        /////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public bool LogOff()
        {
            //if they log off, then we remove the session.  That way, if they access
            //again later they have to log back on in order for their ID to be back
            //in the session!
            Session.Abandon();
            return true;
        }


        /////////////////////////////////////////////////////////
        //EXAMPLE OF: insert query, collecting the primary key of the inserted row
        //				putting a value into a session variable
        /////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public bool CreateAccount(string uid, string pass, string firstName, string lastName)
        {
            //again, this is either gonna work or it won't.  We return this flag to let them
            //know if account creation was successful
            bool success = false;
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //the only thing fancy about this query is SELECT SCOPE_IDENTITY() at the end.  All that
            //does is tell sql server to return the primary key of the last inserted row.
            //we want this, because if the account gets created we will automatically
            //log them on by storing their id in the session.  That's just a design choice.  You could
            //decide that after they create an account they still have to log on seperately.  Whatevs.
            string sqlSelect = "insert into useraccount (username, password, firstname, lastname) " +
                "values(@idValue, @passValue, @fnameValue, @lnameValue)SELECT SCOPE_IDENTITY();";

            SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
            SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.Add("@idValue", System.Data.SqlDbType.NVarChar);
            sqlCommand.Parameters["@idValue"].Value = HttpUtility.UrlDecode(uid);
            sqlCommand.Parameters.Add("@passValue", System.Data.SqlDbType.NVarChar);
            sqlCommand.Parameters["@passValue"].Value = HttpUtility.UrlDecode(pass);
            sqlCommand.Parameters.Add("@fnameValue", System.Data.SqlDbType.NVarChar);
            sqlCommand.Parameters["@fnameValue"].Value = HttpUtility.UrlDecode(firstName);
            sqlCommand.Parameters.Add("@lnameValue", System.Data.SqlDbType.NVarChar);
            sqlCommand.Parameters["@lnameValue"].Value = HttpUtility.UrlDecode(lastName);

            //this time, we're not using a data adapter to fill a data table.  We're just
            //opening the connection, telling our command to "executescalar" which says basically
            //execute the query and just hand me back the number the query returns (the ID, remember?).
            //don't forget to close the connection!
            sqlConnection.Open();
            //we're using a try/catch so that if the query errors out we can handle it gracefully
            //by closing the connection and moving on
            try
            {
                int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
                //these three lines only execute if the command doesn't error out
                //so they won't get logged in unless the row is actually inserted.
                success = true;
                Session["id"] = accountID;
                //this is monkey business that you'll understand when you read the comments in
                //get messages web method
                Session["lastmessageid"] = -1;
            }
            catch (Exception e) { }
            sqlConnection.Close();

            return success;
        }


        /////////////////////////////////////////////////////////
        //EXAMPLE OF: using a session variable to confirm the user is logged in
        //				executing a query that doesn't return anything
        /////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public bool SendMessage(string message)
        {
            bool success = false;
            //here we're checking the id session variable to make sure it's not empty
            //if it is, we won't let them send a message!
            if (Session["id"] != null)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                string sqlSelect = "insert into message (useraccountid, message) " +
                    "values(@idValue, @messageValue);";

                SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
                SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.Add("@idValue", System.Data.SqlDbType.Int);
                //we're using the id session variable for the parameter here.  When
                //we pull stuff out of the session variable it's just a generic object
                //so we have to explicitly type cast it to, in this case, an int
                sqlCommand.Parameters["@idValue"].Value = Convert.ToInt32(Session["id"]);
                sqlCommand.Parameters.Add("@messageValue", System.Data.SqlDbType.NVarChar);
                sqlCommand.Parameters["@messageValue"].Value = HttpUtility.UrlDecode(message);

                sqlConnection.Open();
                //here's another kind of execution, that just tells us the number of rows affected
                //if it's greater than 0, we can assume the row was inserted (the message was sent)
                int affectedRows = sqlCommand.ExecuteNonQuery();
                sqlConnection.Close();
                if (affectedRows > 0)
                {
                    //message was sent, so set flag to true!
                    success = true;
                }
            }
            return success;
        }


        /////////////////////////////////////////////////////////
        //EXAMPLE OF: using session variable to confirm logged in
        //				select query for multiple rows
        //				custom object to organize the data we want to return
        /////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public Message[] GetMessages()
        {
            //check out the return type.  It's an array of Message objects.  You can
            //look at our custom Message class in this solution to see that it's 
            //just a container for public class-level variables.  It's a simple container
            //that asp.net will have no trouble converting into json.  When we return
            //sets of information, it's a good idea to create a custom container class to represent
            //instances (or rows) of that information, and then return an array of those objects.  
            //Keeps everything simple.

            //LOGIC: we don't want to return every message every time, so when folks check for
            //messages we'll keep track of the ID of the last message we send them.  That gets
            //stored up in a session variable.  When they come back, we only send them messages
            //that came in after the last message they've seen.  If they haven't checked yet,
            //then we grab the last 100 messages from the table and give them those (storing the 
            //id of the last one in the session so we're ready for next time!)

            DataTable sqlDt = new DataTable("messages");
            int lastMessageId = -1;
            //they get no messages if they aren't logged in!
            if (Session["id"] != null)
            {
                //if we've stored an ID of the last message we've seen
                //(if they've checked messages already, in other words)
                //then we grab that ID from the session
                if (Session["lastmessageid"] != null)
                {
                    lastMessageId = Convert.ToInt32(Session["lastmessageid"]);
                }
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                string sqlSelect;
                if (lastMessageId == -1)
                {
                    //they haven't seen any messages, so grab the bottom 100
                    sqlSelect = "select * from (select top 100 m.id, m.useraccountid, u.firstname, m.message from message m, useraccount u where m.useraccountid=u.id order by m.id desc) as a order by a.id;";
                }
                else
                {
                    //they've seen some messages, so grab those that came after
                    //the last one they've seen
                    sqlSelect = "select m.id, m.useraccountid, u.firstname, m.message from message m, useraccount u where m.useraccountid=u.id and m.id>@messageId order by m.id desc;";
                }
                SqlConnection sqlConnection = new SqlConnection(sqlConnectString);
                SqlCommand sqlCommand = new SqlCommand(sqlSelect, sqlConnection);

                //if we're grabbing messages after the last one they've seen
                //we need to feed a value into that parameter
                if (lastMessageId != -1)
                {
                    sqlCommand.Parameters.Add("@messageId", System.Data.SqlDbType.Int);
                    sqlCommand.Parameters["@messageId"].Value = lastMessageId;
                }

                //gonna use this to fill a data table
                SqlDataAdapter sqlDa = new SqlDataAdapter(sqlCommand);
                //filling the data table
                sqlDa.Fill(sqlDt);
                if (sqlDt.Rows.Count > 0)
                {
                    //we found some messages that we're going to return, so
                    //let's store the ID of the last message we're going to hand back
                    //for the next time they check for messages.  We'll put it 
                    //in the session so we have access to it later.
                    Session["lastmessageid"] = sqlDt.Rows[sqlDt.Rows.Count - 1]["id"];
                }
            }
            //loop through each row in the dataset, creating instances
            //of our container class Message.  Fill each message with
            //data from the rows, then dump them in a list.
            List<Message> messages = new List<Message>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                messages.Add(new Message
                {
                    id = Convert.ToInt32(sqlDt.Rows[i]["id"]),
                    userName = sqlDt.Rows[i]["firstname"].ToString(),
                    message = sqlDt.Rows[i]["message"].ToString()
                });
            }
            //convert the list of messages to an array and return!
            return messages.ToArray();
        }
    }
}
