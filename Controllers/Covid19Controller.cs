// Name: Jingyan Sun
// Nov 30, 2020
// This service can be used to get, add, update and delete Covid-19
//		statistics from the database.
// The database contains 2 tables. One is used to record country stats.
//		The other is used to record history stats.
// We consume 2 existing services.
// We define 3 HttpGet methods, 2 HttpPost methods, 2 HttpPut methods,
//		and 2 HttpDelete methods.

using System;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using System.Net.Http;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Text; 
using System.IO; 
using System.Text.Json;

namespace mod9_example.Controllers{
    public class Covid19Controller : ControllerBase{
        // We call this method to store data in the database.
        [HttpGet]
        [Route("storedata/")]
        public IActionResult storeData(){
            try{
                // The service endpoint for Service Provider 1 (SP1)
                string serviceURLBase2 = "https://covid-19-statistics.p.rapidapi.com/reports/total?date="; 
                string[] dateArr = {"2020-11-23", "2020-11-24", "2020-11-25", "2020-11-26", "2020-11-27"};
				string serviceURL = "";
                for (int i=0; i<dateArr.Length; i++){
                    serviceURL = serviceURLBase2 + dateArr[i];
				    WebRequest serviceRequest = (WebRequest)WebRequest.Create(serviceURL); 
				    serviceRequest.Method = "GET"; 
				    serviceRequest.ContentLength = 0; 
				    serviceRequest.ContentType = "plain/text"; 
										// Use your own api key here
				    serviceRequest.Headers.Add("x-rapidapi-key", "69413b8873mshf95f8cb33400306p110301jsn3cef58afe473");
				    serviceRequest.Headers.Add("x-rapidapi-host", "covid-19-statistics.p.rapidapi.com");
				    WebResponse serviceResponse = (WebResponse)serviceRequest.GetResponse(); 
				    Stream receiveStream = serviceResponse.GetResponseStream(); 
				    Encoding encode = System.Text.Encoding.GetEncoding("utf-8"); 
				    StreamReader readStream = new StreamReader(receiveStream, encode, true); 
				    string serviceResult = readStream.ReadToEnd(); 
                    var reportInfo = JsonSerializer.Deserialize<Report2>(serviceResult);
                    DataSet dataSet1 = executeSQL("INSERT INTO report VALUES (" +
                                        (char)39 + reportInfo.data.date + (char)39 + "," +
                                        reportInfo.data.recovered + "," +
                                        reportInfo.data.deaths + "," +
                                        reportInfo.data.confirmed + "," +
                                        reportInfo.data.active +
                                        ");","total");
                }

                // The service endpoint for Service Provider 2 (SP2)
                string serviceURLBase1 = "https://covid-19-coronavirus-statistics.p.rapidapi.com/v1/total?country="; 
                string[] countryArr = {"Canada", "USA", "France", "Germany", "Mexico"};
                for (int i=0; i<countryArr.Length; i++){
                    serviceURL = serviceURLBase1 + countryArr[i];
				    WebRequest serviceRequest = (WebRequest)WebRequest.Create(serviceURL); 
				    serviceRequest.Method = "GET"; 
				    serviceRequest.ContentLength = 0; 
				    serviceRequest.ContentType = "plain/text"; 
				    serviceRequest.Headers.Add("x-rapidapi-key", "69413b8873mshf95f8cb33400306p110301jsn3cef58afe473");
				    serviceRequest.Headers.Add("x-rapidapi-host", "covid-19-coronavirus-statistics.p.rapidapi.com");
				    serviceRequest.Headers.Add("country", countryArr[i]);
				    WebResponse serviceResponse = (WebResponse)serviceRequest.GetResponse(); 
				    Stream receiveStream = serviceResponse.GetResponseStream(); 
				    Encoding encode = System.Text.Encoding.GetEncoding("utf-8"); 
				    StreamReader readStream = new StreamReader(receiveStream, encode, true); 
				    string serviceResult = readStream.ReadToEnd(); 
                    var totalInfo = JsonSerializer.Deserialize<Total2>(serviceResult);
                    DataSet dataSet1 = executeSQL("INSERT INTO total VALUES (" +
                                        (char)39 + totalInfo.data.location + (char)39 + "," +
                                        (char)39 + totalInfo.data.lastReported + (char)39 + "," +
                                        totalInfo.data.recovered + "," +
                                        totalInfo.data.deaths + "," +
                                        totalInfo.data.confirmed +
                                        ");","total");
                }
				return Ok("The data has been stored successfully.");
            }
            catch{
                return BadRequest("Failed to store the data in the database. Maybe it has already been stored.");
            }
        }

        // Get the country stats
        [HttpGet] 
		[Route("getCountry")]
		[Route("getCountry/{country}")]
        public IActionResult getCountry(string country){
            if (string.IsNullOrEmpty(country)){
                DataSet stats = executeSQL("SELECT * FROM total;","total");
	            int countRecords = stats.Tables[0].Rows.Count;
	            if (countRecords == 0){
		            return BadRequest("There is no record in the table.");
	            }
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
	            return Ok(stats);
            }else{
				// Uppercase the first letter
                country=char.ToUpper(country[0]) + country.Substring(1);
                DataSet stats = executeSQL("SELECT * FROM total WHERE country ="+(char)39+country+(char)39+";","total");
	            int countRecords = stats.Tables[0].Rows.Count;
	            if (countRecords == 0){
		            return BadRequest("Sorry. There is no matching record.");
	            }
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
	            return Ok(stats);
            }
        }

        // Get the history stats
        [HttpGet] 
		[Route("getHistory")]
		[Route("getHistory/{date}")]
        public IActionResult getHistory(string date){
            if (string.IsNullOrEmpty(date)){
                DataSet stats = executeSQL("SELECT * FROM report;","report");
	            int countRecords = stats.Tables[0].Rows.Count;
	            if (countRecords == 0){
		            return BadRequest("There is no record in the table.");
	            }
				HttpContext.Response.Headers.Add("Status","success");
	            return Ok(stats);
            }else{
                // change the format of the date if necessary
				DateTime date1 = DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture);
				date = date1.ToString("yyyy-MM-dd");
                DataSet stats = executeSQL("SELECT * FROM report WHERE date ="+(char)39+date+(char)39+";","report");
	            int countRecords = stats.Tables[0].Rows.Count;
	            if (countRecords == 0){
		            return BadRequest("Sorry. There is no matching record.");
	            }
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
	            return Ok(stats);
            }
        }

        // Insert new country stats into the database
        [HttpPost]
        [Route("addCountry/")]
        public IActionResult addCountry([FromBody]Total total){
            string sqlStatement="";
	        try{
		        sqlStatement = "INSERT INTO total VALUES (" +
			        (char)39 + total.location + (char)39 + "," +
			        (char)39 + total.lastReported + (char)39 + "," +
			        total.recovered + "," +
			        total.deaths + "," +
			        total.confirmed + ");";
		        executeSQL(sqlStatement,"total");
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
		        return Ok("The record has been added successfully.");
	        }
	        catch{
		        return BadRequest("Failed to add the record.");
	        }
        }

        // Insert new history stats into the database
        [HttpPost]
        [Route("addHistory/")]
        public IActionResult addHistory([FromBody]Report report){
            string sqlStatement="";
	        try{
		        sqlStatement = "INSERT INTO report VALUES (" +
			        (char)39 + report.date + (char)39 + "," +
			        report.confirmed + "," +
			        report.deaths + "," +
			        report.recovered + "," +
			        report.active + ");";
		        executeSQL(sqlStatement,"report");
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
		        return Ok("The record has been added successfully.");
	        }
	        catch{
		        return BadRequest("Failed to add the record.");
	        }
        }

        // Update the country stats
        [HttpPut]
        [Route("updateCountry/{country}")]
        public IActionResult updateCountry(string country, [FromBody]Total total){
            string sqlStatement="";
	        try{
		        sqlStatement = "UPDATE total SET lastReported=" +
			        (char)39 + total.lastReported + (char)39 + ", recovered=" +
			        total.recovered + ", deaths=" +
			        total.deaths + ", confirmed=" +
			        total.confirmed + " WHERE country="+
					(char)39 + country + (char)39 + ";";
		        executeSQL(sqlStatement,"total");
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
		        return Ok("The record has been updated successfully.");
	        }
	        catch{
		        return BadRequest("Failed to update the record.");
	        }
        }

        // Update the history stats into the database
        [HttpPut]
        [Route("updateHistory/{date}")]
        public IActionResult updateHistory(string date, [FromBody]Report report){
            string sqlStatement="";
	        try{
		        sqlStatement = "UPDATE report SET confirmed=" +
			        (char)39 + report.confirmed + (char)39 + ", deaths=" +
			        report.deaths + ", recovered=" +
			        report.recovered + ", active=" +
					report.active + " WHERE date="+
					(char)39 + date + (char)39 + ";";
		        executeSQL(sqlStatement,"report");
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
		        return Ok("The record has been updated successfully.");
	        }
	        catch{
		        return BadRequest("Failed to update the record.");
	        }
        }

		// Delete the country stats
        [HttpDelete]
        [Route("deleteCountry/{country}")]
        public IActionResult deleteCountry(string country){
            string sqlStatement="";
	        try{
		        sqlStatement = "DELETE FROM total WHERE country='" + country + "';";
		        executeSQL(sqlStatement,"total");
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
		        return Ok("The record has been deleted successfully.");
	        }
	        catch{
		        return BadRequest("Failed to delete the record.");
	        }
        }

		// Delete the history stats
        [HttpDelete]
        [Route("deleteHistory/{date}")]
        public IActionResult deleteHistory(string date){
            string sqlStatement="";
	        try{
		        sqlStatement = "DELETE FROM report WHERE date='" + date + "';";
		        executeSQL(sqlStatement,"report");
				// Add "Status: success" to Response Header
				HttpContext.Response.Headers.Add("Status","success");
		        return Ok("The record has been deleted successfully.");
	        }
	        catch{
		        return BadRequest("Failed to delete the record.");
	        }
        }

		// Execute the sql statement
        [ApiExplorerSettings(IgnoreApi = true)]
        [NonAction]
        public DataSet executeSQL(string sqlStatement, string table){
            string connStr = "server=localhost; port=3306; user=root; password=FS1kXFWqH3cybQ; database=assign5covid19; ";
            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlDataAdapter sqlAdapter = new MySqlDataAdapter(sqlStatement, conn);
            DataSet myResultSet = new DataSet();
            sqlAdapter.Fill(myResultSet, table);
            return myResultSet;
        }
    }
}
