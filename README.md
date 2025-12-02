# AI Mission Presentation

## Problem being solved

For each PEO deadletter, what was the root problem, where did it happen, and what is the recommended fix. This involves multiple pieces working together:

	* AI Agent accessing:
	* SQL Database
	* Application Insights
	* GitHub
	* Teams

### Solution Process

The function that throws the deadletter could be caused by an Affinity API it calls. 

	* Identify the repo for the deadletter
	* Identify the App from the repo
	* Identify the excpetion from the App AppInsight logs
	* Identify the Repo where the exception occurred
	* Reading and analyzing the repo GitHub code
	* Marking the deadletter record as processed

## AI Agents

* Azure AI Foundary
	- Pros
		- Low Code
		- Handles memory
		- Automatically handles many of the low level details
	- Cons
		- Cannot be implemented in code
* Semantic Kernel
	- Pros
		- Implements in a function
		- Very Flexible
		- Illustrates how to implement agent in code
	- Cons
		- Have to handle memory
		- Have to handle all the details, i.e. connections to GitHub, AppInsights, etc.
		- Have to write the queries, kusto & SQL
	- Two approaches
		- Workflow
			- Agent only used to analyze code
		- Full Agent
			- Agent given connection to all the services and determines how to get to the code and analyze it.

### Issues

* Memory
	- Code Stucture
	- Previous issues/fixes
	
## Learnings

	* Logging Standards needed
	* Use of Correlation Ids
		- Fortunately, due to some Lee put in place, was able to use these for PEO
	
## Mission Status 

Where things stand based on the time spent so far. Most of my time was spent on research, with a little spent on building Semantic Kernel Frameworks, 
but no working POC yet.

