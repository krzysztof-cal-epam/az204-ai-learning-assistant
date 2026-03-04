## AZ-204 Quiz Generator (PoC) — Demo steps

This repository ships a minimal static UI served directly by the ASP.NET Core API host.  
The UI calls the existing endpoints via same-origin relative URLs to avoid CORS and extra infrastructure.

### 1. Build and test (optional but recommended)

From the repository root:

```bash
dotnet build
dotnet test
```

### 2. Run the API with the static UI

From the repository root:

```bash
dotnet run --project Api --urls http://localhost:5099
```

> The `Api` project is configured to serve static files from `Api/wwwroot`.
> A default `index.html` is used when you visit the root URL.

### 3. Open the demo UI

In a browser, navigate to:

- `http://localhost:5099/`

You should see the **“AZ-204 Quiz Generator (PoC)”** page.

### 4. Health check call

In the UI:

1. Click **Health**.
2. Observe:
   - The status pill shows a loading state.
   - The **Output** panel shows:
     - A summary with `Mode` and `ResponsesUrl`.
     - The full JSON from `GET /api/LearningAssistant/Health`.

### 5. Generate quiz call

In the UI:

1. Ensure a valid topic is selected (for example `azure-functions`).
2. Leave **Question count** at the default `3` (or any value between 1 and 10).
3. Click **Generate**.
4. Observe:
   - The status pill shows **Loading…** while waiting.
   - Once complete, the **Output** panel shows:
     - The quiz topic.
     - A list of questions.
     - Options A–D for each question.
     - The correct option marked with a ✅ icon using `correctOptionIndex`.

If the model is slow, the UI stays on **Loading…** until a response arrives.

### 6. Invalid topic example (400)

You can demonstrate backend validation using either the UI or `curl`.

#### 6.1. Via UI

1. Manually type an invalid topic in the browser dev tools:
   - In the **Console**, run:
     ```js
     document.getElementById("topic").value = "not-a-valid-topic";
     ```
2. Click **Generate**.
3. The UI should:
   - Show an HTTP 400 status in the error block.
   - Render the JSON error payload from the API.

#### 6.2. Via `curl`

From a terminal:

```bash
curl -i ^
  -H "Content-Type: application/json" ^
  -d "{\"topic\":\"not-a-valid-topic\",\"questionCount\":3}" ^
  http://localhost:5099/api/LearningAssistant/GenerateQuiz
```

Expected response (simplified):

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8

{"error":"invalid_topic","message":"Requested topic is not allowed."}
```

### 7. Notes

- The UI only uses **relative URLs** (`/api/LearningAssistant/Health` and `/api/LearningAssistant/GenerateQuiz`) so it runs against whichever host/port the API uses.
- If the API ever returns a non-JSON body, the UI falls back to showing the raw response in a `<pre>` block to keep errors readable.

