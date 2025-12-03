# Semantic Kernel Full Agent Solution

## Problem being solved

For each PEO deadletter, what is the root problem, where did it happen, and what is the recommended fix. This involves multiple pieces working together:

	* AI Agent accessing:
	* SQL Database
	* Application Insights
	* GitHub
	* Teams

### Solution Process

The function that throws the deadletter could be caused by a deeper process than the function that threw the dead letter. For example, an Affinity API 
the function calls could have thrown the original exception. A developer might perform the following steps to find a solution.

	* Identify the repo for the deadletter
	* Identify the App from the repo
	* Identify the excpetion from the App AppInsight logs
	* Identify the Repo where the exception occurred
	* Reading and analyzing the repo GitHub code
	* Marking the deadletter record as processed

## AI Agents

Went down several paths trying to understand the various AI technologies. But two always kept coming up in one form or another, Semantic Kernel and
Azure AI Agent Service (Azure AI Foundary). Depending on when and how I asked the question, I was getting different answers as to which was better.
Semantic Kernel is a Microsoft SDK and does provide some flexibility, as originaly touted, but has drawbacks as I later learned. 

Trying to get a handle on all the terms and exactly what they truely mean. Going down the path of Semantic Kernel certainly helped me get a better handle 
on how things Workflow as you have more control and are forced to implement more things. Learnig the difference between Chatbots, agents, full vs workflows,
LLMs, etc. 

* Azure AI Foundary
	- Pros
		- Low Code
		- Memory built-in
		- Automatically handles many of the low level details like queries, etc.
	- Cons
		- Cannot be implemented in code
		- Cannot be version controlled
* Semantic Kernel
	- Pros
		- Implements in a function
		- Very Flexible
		- Illustrates how to implement agent in code
		- Can be version controlled
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
	- Application and Process Code Stucture
	- Previous issues/fixes
	
## Learnings

	* Copilot Instructions files
	* Logging Standards needed
	* Use of Correlation Ids
		- Fortunately, due to some Lee put in place, was able to use these for PEO
	
## AI Mission Status 

Where things stand based on the time spent so far. Most of my time was spent on research, with a little spent on building Semantic Kernel Frameworks, 
but no working POC yet.

## Proposal

Because I am still lacking understanding of the Azure AI Foundary, but other are working on understanding. Proposing that the Semantic Kernal Full Agent 
approach be implemented for the following reasons:

	* Gain understanding in how to embed agents in AFfinity's code base with greater control
	* Be able to track the various versions of this process 
	* Gain deeper insights in how to develop our applications (ex. logging, etc.)

## Full Agent Design Diagrams

The design for the current version of the Semantic Kernel Full Agent Framework are illustrated in the following diagrams.

### Data Flow

```mermaid
flowchart TD

    %% Triggers & entry
    subgraph AzureFunctions["Azure Function App - Isolated .NET 8"]
        TMR[Timer Trigger - Cron 0 */5 * * * *]
        FUNC[DeadletterAgentFunction RunAsync]
    end

    subgraph SQL["Affinity SQL Database"]
        DLQ[ServiceBusDeadletters Table<br/>ProcessedFlag = 0]
    end

    subgraph Agent["DeadletterSkAgent<br/>Semantic Kernel and Azure OpenAI"]
        AGENT[RunForCorrelationAsync<br/>Build goal and KernelArguments]
        KERNEL[Kernel InvokePromptAsync<br/>FunctionChoiceBehavior Auto]
    end

    subgraph SKPlugins["Semantic Kernel Plugins - Tools"]
        P_SQL[DeadletterSqlPlugin<br/>Mark Deadletter Group Processed]
        P_AI[AppInsightsPlugin<br/>Get Trace and Exception]
        P_GH[GitHubPlugin<br/>Resolve Code Context]
        P_NOTIF[NotificationPlugin<br/>Send Teams Notification]
    end

    subgraph AppInsights["Application Insights - Log Analytics Workspace"]
        TRACES[Traces Table<br/>customDimensions AffinityCorrelationId]
        EXC[Exceptions Table]
    end

    subgraph GitHub["GitHub Repository<br/>Lockton Affinity"]
        CODE[Source Files]
    end

    subgraph Teams["Microsoft Teams"]
        TEAMSMSG[Incident Message Posted<br/>Deadletter Analysis]
    end

    subgraph AzureOpenAI["Azure OpenAI Service"]
        LLM[gpt-4o Model<br/>LLM Reasoning & Tool Selection]
    end

    %% Timer trigger
    TMR --> FUNC

    %% Function fetches DLQ
    FUNC -->|Query unprocessed deadletters| DLQ
    DLQ -->|Return rows| FUNC

    FUNC -->|Group by CustomCorrelationId<br/>Filter count >= 10| AGENT

    %% Agent + Kernel orchestration
    AGENT -->|Build goal prompt<br/>Build KernelArguments| KERNEL
    KERNEL -->|Send prompt and tool definitions| LLM

    %% LLM tool calls
    LLM -->|Call AppInsightsPlugin<br/>GetLatestTraceByCorrelationId| P_AI
    P_AI --> TRACES
    TRACES --> P_AI
    P_AI --> LLM

    LLM -->|Call AppInsightsPlugin<br/>GetExceptionByOperationId| P_AI
    P_AI --> EXC
    EXC --> P_AI
    P_AI --> LLM

    LLM -->|Call GitHubPlugin<br/>ResolveContext| P_GH
    P_GH --> CODE
    CODE --> P_GH
    P_GH --> LLM
 
    %%LLM -->|Generate Markdown Summary| LLM

    %% Notify and mark processed
    LLM -->|Call NotificationPlugin<br/>SendMessage| P_NOTIF
    P_NOTIF --> TEAMSMSG
    TEAMSMSG --> P_NOTIF
    P_NOTIF --> LLM

    LLM -->|Optional Tool Call<br/>Mark Group Processed| P_SQL
    P_SQL --> DLQ
```

### Sequence Diagrams

A sequenceDiagram version that shows one concrete run for a single CorrelationId hitting the “10 in last hour” threshold, step-by-step.

```mermaid
sequenceDiagram
    participant Timer as TimerTrigger<br/>("0 */5 * * * *")
    participant Func as DeadletterAgentFunction
    participant SqlRepo as ISqlDeadletterRepository<br/>(SqlDeadletterRepository)
    participant Agent as DeadletterSkAgent
    participant Kernel as Semantic Kernel<br/>Kernel
    participant LLM as Azure OpenAI<br/>(gpt-4o)
    participant AppI as AppInsightsPlugin<br/>(IAppInsightsClient)
    participant GH as GitHubPlugin<br/>(IGitHubClient)
    participant Notify as NotificationPlugin<br/>(INotificationClient)
    participant Teams as Microsoft Teams

    %% Timer fires
    Timer->>Func: RunAsync(timerInfo, ct)

    Note over Func: Compute oneHourAgo = UtcNow - 1 hour

    Func->>SqlRepo: GetUnprocessedDeadlettersSinceAsync(oneHourAgo)
    SqlRepo-->>Func: List<DeadletterRecord>

    Note over Func: Group records by<br/>CustomCorrelationId<br/>Filter groups where count >= 10

    Func->>Agent: RunForCorrelationAsync(correlationId,<br/>groupList, oneHourAgo, ct)

    Note over Agent: Build goal prompt +<br/>KernelArguments<br/>(incidentCorrelationId, counts,<br/>time window, payload JSON)

    Agent->>Kernel: InvokePromptAsync(goal, args, ct)
    Kernel->>LLM: Send prompt + tool schema<br/>(plugins exposed)

    Note over LLM: Thinks about goal and decides<br/>which tools to call using<br/>FunctionChoiceBehavior.Auto()

    %% Tool call 1: get trace by correlation id
    LLM->>AppI: GetLatestTraceByCorrelationIdAsync(correlationId)
    AppI-->>LLM: TraceInfo (operation_Id, message,...)

    %% Tool call 2: get exception by operation id
    LLM->>AppI: GetExceptionByOperationIdAsync(operation_Id)
    AppI-->>LLM: ExceptionInfo (stackTrace, type,...)

    %% Tool call 3: resolve code from stack trace
    LLM->>GH: ResolveCodeFromStackTraceAsync(stackTrace)
    GH-->>LLM: CodeContext (file, line, GitHubUrl)

    Note over LLM: Analyze traces + exception + code<br/>Generate incident Markdown summary

    %% Tool call 4: notify developers
    LLM->>Notify: NotifyDeveloperAsync(representativeId,<br/>correlationId, count,<br/>summaryMarkdown)
    Notify->>Teams: POST webhook<br/>(incident card / message)
    Teams-->>Notify: 200 OK
    Notify-->>LLM: Notification sent

    %% (Optional) Tool call 5: mark group processed
    LLM->>Kernel: Call DeadletterSqlPlugin.<br/>MarkDeadletterGroupAsProcessedAsync(correlationId, oneHourAgoIso)
    Kernel->>SqlRepo: MarkGroupAsProcessedAsync(correlationId, oneHourAgo)
    SqlRepo-->>Kernel: OK
    Kernel-->>LLM: OK

    LLM-->>Kernel: Final LLM response<br/>(summary + tool traces)
    Kernel-->>Agent: FunctionResult
    Agent-->>Func: Task completed for correlationId
    Func-->>Timer: Function run completes
```
### Sequence Diagrams - tool-calling loop

The sequenceDiagram just for the tool-calling loop (Kernel ↔ LLM ↔ plugins)

```mermaid
sequenceDiagram
    %% Participants
    participant Agent as DeadletterSkAgent
    participant Kernel as Semantic Kernel<br/>Kernel
    participant LLM as Azure OpenAI<br/>(gpt-4o)
    participant AppI as AppInsightsPlugin
    participant GH as GitHubPlugin
    participant Notify as NotificationPlugin
    participant SqlPlug as DeadletterSqlPlugin

    %% Agent calls Kernel
    Agent->>Kernel: InvokePromptAsync(goal, args,<br/>FunctionChoiceBehavior.Auto)

    %% Kernel sends prompt + tool schema
    Kernel->>LLM: Prompt + available tools<br/>(plugin functions)

    Note over LLM: 1️⃣ Read goal & context<br/>2️⃣ Decide which tool to call first

    %% Tool call 1 - App Insights trace lookup
    LLM->>Kernel: Tool call request:<br/>AppInsightsPlugin.GetLatestTraceByCorrelationIdAsync(correlationId)
    Kernel->>AppI: Invoke GetLatestTraceByCorrelationIdAsync(correlationId)
    AppI-->>Kernel: TraceInfo (operation_Id, message, severity)
    Kernel-->>LLM: Tool result: TraceInfo JSON

    Note over LLM: Incorporate trace info<br/>and reason about next step

    %% Tool call 2 - App Insights exception lookup
    LLM->>Kernel: Tool call request:<br/>AppInsightsPlugin.GetExceptionByOperationIdAsync(operation_Id)
    Kernel->>AppI: Invoke GetExceptionByOperationIdAsync(operation_Id)
    AppI-->>Kernel: ExceptionInfo (type, message, stackTrace)
    Kernel-->>LLM: Tool result: ExceptionInfo JSON

    Note over LLM: Now has stack trace<br/>→ needs code location

    %% Tool call 3 - GitHub code resolution
    LLM->>Kernel: Tool call request:<br/>GitHubPlugin.ResolveCodeFromStackTraceAsync(stackTrace)
    Kernel->>GH: Invoke ResolveCodeFromStackTraceAsync(stackTrace)
    GH-->>Kernel: CodeContext (filePath, lineNumber, GitHubUrl)
    Kernel-->>LLM: Tool result: CodeContext JSON

    Note over LLM: Combine trace + exception + code<br/>Generate root-cause analysis Markdown

    %% Tool call 4 - Notify devs
    LLM->>Kernel: Tool call request:<br/>NotificationPlugin.NotifyDeveloperAsync(representativeId,<br/>correlationId, count, summaryMarkdown)
    Kernel->>Notify: Invoke NotifyDeveloperAsync(...)
    Notify-->>Kernel: Notification sent (OK)
    Kernel-->>LLM: Tool result: success status

    %% Optional: mark processed via SQL plugin
    alt LLM decides to mark group processed
        LLM->>Kernel: Tool call request:<br/>DeadletterSqlPlugin.MarkDeadletterGroupAsProcessedAsync(correlationId, windowStartUtcIso)
        Kernel->>SqlPlug: Invoke MarkDeadletterGroupAsProcessedAsync(...)
        SqlPlug-->>Kernel: OK
        Kernel-->>LLM: Tool result: OK
    end

    Note over LLM: Finalize answer text<br/>(may include summary or just rely on tools)

    LLM-->>Kernel: Final LLM message<br/>(FunctionResult content)
    Kernel-->>Agent: FunctionResult returned
```

# Semantic Kernel Workflow w/Agent Solution

Simplier more deterministic approach.

## Workflow w/Agent Design

### Dataflow Diagram(s)

```mermaid
flowchart TD
    %% =============================
    %% Deadletter Diagnostic AI Agent
    %% =============================

    %% Triggers & Entry
    TMR[Timer Trigger<br/>e.g. every 5 minutes] --> FN_START[Azure Function<br/>Deadletter Diagnostic Agent]

    %% Step 1: Read new deadletters from SQL
    FN_START --> SQL[Azure SQL DB<br/>Deadletter Table]
    SQL -->|SELECT new rows<br/>WHERE ProcessedFlag = 0| DLQ_ROWS[New Deadletter Records]

    DLQ_ROWS -->|For each record| EXTRACT_IDS[Extract identifiers<br/>CustomCorrelationId and others]

    %% Step 2: Query App Insights for traces
    EXTRACT_IDS --> AI_TRACES[Azure Application Insights<br/>Traces]
    AI_TRACES -->|Kusto query by<br/>CustomCorrelationId| TRACE_RESULT[Trace Results]
    TRACE_RESULT -->|Get operation_Id| GET_OPID[Resolve operation_Id]

    %% Step 3: Query App Insights for exceptions
    GET_OPID --> AI_EXC[Azure Application Insights<br/>Exceptions]
    AI_EXC -->|Kusto query by<br/>operation_Id| EXC_RESULT[Exception and Stack Trace]

    %% Step 4: Map stack trace to GitHub source
    EXC_RESULT --> MAP_STACK[Parse stack trace<br/>assembly class method file line]
    MAP_STACK --> MAP_PATHS[Map build paths →<br/>repo paths]
    MAP_PATHS --> GITHUB[GitHub API<br/>Affinity repos]
    GITHUB --> CODE_CTX[Code context<br/>file snippet and links]

    %% Step 5: AI reasoning
    EXTRACT_IDS --> AGG_DATA[Aggregate context<br/>DLQ payload IDs timestamps]
    TRACE_RESULT --> AGG_DATA
    EXC_RESULT --> AGG_DATA
    CODE_CTX --> AGG_DATA

    subgraph LLM[AI Reasoning Engine<br/>Azure OpenAI / LLM]
        AGG_DATA --> PROMPT[Construct prompt with<br/>logs stack and code]
        PROMPT --> CALL_LLM[Call model<br/>analyze and suggest fixes]
        CALL_LLM --> LLM_OUT[AI Analysis and<br/>Suggested Fixes]
    end

    %% Step 6: Notify developers
    LLM_OUT --> FORMAT_MSG[Build dev-friendly summary<br/>Markdown or HTML]
    FORMAT_MSG --> NOTIFY_CHOICE{Notification channel}

    NOTIFY_CHOICE -->|Teams| TEAMS[Post to Teams channel<br/>webhook or Graph]
    NOTIFY_CHOICE -->|Email| EMAIL[Send email<br/>SendGrid or ACS]
    NOTIFY_CHOICE -->|Slack| SLACK[Post to Slack channel<br/>webhook]

    %% Step 7: Mark record as processed
    NOTIFY_CHOICE --> UPDATE_SQL[Update Deadletter row<br/>set ProcessedFlag = 1]
    UPDATE_SQL --> SQL_DONE((Deadletter marked<br/>as processed))

    %% Optional: Feedback loop / metrics
    LLM_OUT --> METRICS[Log AI decision and context<br/>to App Insights or table]
    METRICS -->|Future tuning and audits| FN_START

    %% Legend styling
    classDef service fill:#e3f2fd,stroke:#1e88e5,stroke-width:1px,color:#0d47a1;
    classDef data fill:#fff3e0,stroke:#fb8c00,stroke-width:1px,color:#e65100;
    classDef ai fill:#f3e5f5,stroke:#8e24aa,stroke-width:1px,color:#4a148c;
    classDef notif fill:#e8f5e9,stroke:#43a047,stroke-width:1px,color:#1b5e20;

    class TMR,FN_START,SQL,AI_TRACES,AI_EXC,GITHUB,TEAMS,EMAIL,SLACK service;
    class DLQ_ROWS,EXTRACT_IDS,TRACE_RESULT,GET_OPID,EXC_RESULT,MAP_STACK,MAP_PATHS,CODE_CTX,AGG_DATA,FORMAT_MSG,UPDATE_SQL,SQL_DONE,METRICS data;
    class LLM,PROMPT,CALL_LLM,LLM_OUT ai;
    class NOTIFY_CHOICE notif;

```

### Sequence Diagrams

```mermaid
sequenceDiagram
    %% =============================
    %% Deadletter Diagnostic AI Agent - Sequence
    %% =============================

    participant Timer as Timer Trigger<br/>(Schedule)
    participant Func as Azure Function<br/>(Deadletter Agent)
    participant Sql as Azure SQL DB<br/>(Deadletter Table)
    participant AppTraces as App Insights<br/>(traces)
    participant AppExc as App Insights<br/>(exceptions)
    participant GitHub as GitHub API<br/>(Code Repo)
    participant LLM as Azure OpenAI<br/>(LLM)
    participant Notify as Notification Service<br/>(Teams/Email/Slack)

    %% 1. Timer triggers the function
    Timer->>Func: Invoke on schedule (e.g. every 5 min)

    %% 2. Function queries SQL for new deadletters
    Func->>Sql: SELECT deadletters<br/>WHERE ProcessedFlag = 0
    Sql-->>Func: Deadletter record list

    %% 3. For each deadletter record
    loop For each deadletter record
        Func->>Func: Extract CustomCorrelationId<br/>and other IDs

        %% 3a. Get traces by correlation id
        Func->>AppTraces: Kusto query by CustomCorrelationId
        AppTraces-->>Func: Trace result(s)<br/>with operation_Id

        Func->>Func: Extract operation_Id

        %% 3b. Get exceptions by operation id
        Func->>AppExc: Kusto query by operation_Id
        AppExc-->>Func: Exception + stack trace

        %% 3c. Map stack trace to GitHub source
        Func->>Func: Parse stack frames<br/>(file, method, line)
        Func->>GitHub: Request file info / build link<br/>(repo, path, line)
        GitHub-->>Func: Code context / URL(s)

        %% 4. Call LLM with all context
        Func->>LLM: Send DLQ payload, traces,<br/>exception, stack, code
        LLM-->>Func: Analysis + remediation suggestions

        %% 5. Notify developers
        Func->>Func: Build notification message<br/>(Markdown/HTML)

        alt Notify via Teams
            Func->>Notify: Post via Teams webhook
        else Notify via Email
            Func->>Notify: Send via SendGrid/ACS
        else Notify via Slack
            Func->>Notify: Post via Slack webhook
        end

        %% 6. Mark record as processed
        Func->>Sql: UPDATE deadletter<br/>SET ProcessedFlag = 1
        Sql-->>Func: Update OK
    end

    %% Optional: Log diagnostics for tuning
    Func->>AppTraces: Log "AI Diagnostic Performed"
    AppTraces-->>Func: Ack
```
