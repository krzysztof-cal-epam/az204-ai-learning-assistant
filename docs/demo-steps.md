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

### 5. Generate quiz call + interactive answering

In the UI:

1. Ensure a valid topic is selected (for example `azure-functions`).
2. Leave **Question count** at the default `3` (or any value between 1 and 10).
3. Click **Generate**.
4. Observe:
   - The status pill shows **Loading…** while waiting.
   - Once complete, the **Output** panel shows:
     - The quiz topic.
     - A list of questions.
     - Options A–D for each question as clickable buttons.
   - **No option is highlighted as correct** and there is **no ✅ icon** at this stage.

5. For Question 1:
   - Click one of the answer options.
   - Observe:
     - The clicked option stays visually selected.
     - A status line appears under the question saying **Correct** or **Incorrect**.
     - An **Explain** button appears for that question.
   - Click a different option:
     - The selection moves to the newly clicked option.
     - The status line updates to reflect the new choice (Correct/Incorrect).

6. While a question is selected, click **Explain**:
   - The button shows a loading state (e.g. “Explain (loading…)”) and is disabled during the call.
   - After a short time, an explanation text appears under the question.
   - If the backend returns a 400 or 502 for Explain, an error message appears directly under that question, and the global status pill shows an error state.

7. Click **Generate** again with the same or different topic:
   - The previous quiz, selections, explanations, and per-question errors are cleared.
   - A fresh quiz is rendered with no pre-selected answers and no ✅ icons.

If the model is slow, the UI stays on **Loading…** until a response arrives.

### 6. Custom topic input (unhappy path demo)

The UI now supports typing a custom topic to trigger backend allowlist validation.

#### 6.1. Happy path with allowed topic

1. Select `azure-functions` from the dropdown.
2. Click **Generate**.
3. Select an answer for a question.
4. Click **Explain**.
5. Observe success: explanation appears without errors.

#### 6.2. Unhappy path with disallowed custom topic

1. Select **Custom (type your own…)** from the dropdown.
2. Type `bitcoin` in the custom topic input field.
3. Click **Generate**.
4. Observe:
   - The UI shows an HTTP 400 error.
   - The error payload includes `{"error":"invalid_topic","message":"Requested topic is not allowed."}`.

#### 6.3. Client-side validation for empty custom topic

1. Select **Custom (type your own…)** from the dropdown.
2. Leave the custom topic input empty.
3. Click **Generate**.
4. Observe:
   - No network request is made.
   - The UI shows a validation error: "Please enter a custom topic."

### 7. Invalid topic example (400)

You can demonstrate backend validation using either the UI or `curl`.

#### 7.1. Via UI

1. Manually type an invalid topic in the browser dev tools:
   - In the **Console**, run:
     ```js
     document.getElementById("topic").value = "not-a-valid-topic";
     ```
2. Click **Generate**.
3. The UI should:
   - Show an HTTP 400 status in the error block.
   - Render the JSON error payload from the API.

#### 7.2. Via `curl`

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

### 8. Notes

- The UI only uses **relative URLs** (`/api/LearningAssistant/Health` and `/api/LearningAssistant/GenerateQuiz`) so it runs against whichever host/port the API uses.
- If the API ever returns a non-JSON body, the UI falls back to showing the raw response in a `<pre>` block to keep errors readable.

