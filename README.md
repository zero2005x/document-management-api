<a name="readme-top"></a>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">Document Management API - Technical Case Study</h1>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#objective">Objective</a>
    </li>
    <li><a href="#context">Context</a></li>
    <li>
      <a href="#the-5-ws">The 5 Ws</a>
    </li>
    <li><a href="#user-stories">User Stories</a></li>
    <li><a href="#essential-features">Essential Features</a></li>
    <li><a href="#technical-requirements">Technical Requirements</a></li>
    <li><a href="#endpoint-documentation">Endpoint Documentation</a></li>
    <li><a href="#built-with">Built With</a></li>
  </ol>
</details>

<h2 id="objective">Objective</h2>

<p>
  To build a backend API for a Document Management App with a frontend using React and Tailwind CSS. The API will handle document upload, retrieval, deletion, preview, download, and sharing functionalities.
</p>

<h2 id="context">Context</h2>

<p>
  The Document Management App allows users to upload, view, and download documents. The app consists of a frontend built with React and Tailwind CSS and a backend API developed using ASP.NET Core. The documents are stored in Azure Blob Storage, and their metadata is stored in a PostgreSQL database.
</p>

<h2 id="the-5-ws">The 5 Ws</h2>

<ul>
  <li>
    Who? — Users who need a centralized system to manage their documents effectively.
  </li>
  <li>
    What? — A backend API for document management with upload, retrieval, deletion, preview, download, and sharing features.
  </li>
  <li>
    When? — Users interact with the app to perform document-related tasks.
  </li>
  <li>
    Where? — The API is hosted on a server and accessible through HTTP requests.
  </li>
  <li>
    Why? — Simplifies document management by providing a user-friendly interface and robust backend functionality.
  </li>
</ul>

<h2 id="user-stories">User Stories</h2>

- As a user, I should be able to retrieve a list of available documents.
- As a user, I should be able to upload documents.
- As a user, I should be able to delete documents.
- As a user, I should be able to preview documents.
- As a user, I should be able to download documents.
- As a user, I should be able to share documents with others.


<h2 id="essential-features">Essential Features</h2>

<ol>
  <li>List Available Documents</li>
  <li>Document Upload Functionality</li>
  <li>Delete Documents Functionality</li>
  <li>Document Preview Functionality</li>
  <li>Document Download Functionality</li>
  <li>Document Sharing Functionality</li>
</ol>

<h2 id="technical-requirements">Technical Requirements</h2>

<p>
  The backend API should be implemented using ASP.NET Core and include the necessary endpoints and logic for document management.
</p>

## Installation

To run the Document Management API locally, follow these steps:

1. Clone the repository: `git clone https://github.com/rmoise/document-management-api.git`
2. Navigate to the project directory: `cd document-management-api`
3. Install the dependencies: `dotnet restore`
4. Set up the required environment variables for Azure Blob Storage and PostgreSQL.
5. Start the API: `dotnet run`
6. The API will be accessible at `http://localhost:5000`.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Endpoint Documentation

| Endpoint                                      | Description                                     |
| --------------------------------------------- | ----------------------------------------------- |
| `GET /api/documents`                          | Retrieve the list of available documents         |
| `POST /api/documents`                         | Upload a document                                |
| `DELETE /api/documents/{id}`                   | Delete a document by ID                          |
| `GET /api/documents/{id}/preview`              | Retrieve a document preview by ID                |
| `GET /api/documents/{id}/download`             | Generate a secure download link for a document   |
| `GET /api/documents/{id}/share`                | Generate a public sharing link for a document    |

<h2 id="built-with">Built With</h2>

### Built With

<!-- prettier-ignore -->
* [![React](https://img.shields.io/badge/React-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://reactjs.org/)
* [![Tailwind CSS](https://img.shields.io/badge/Tailwind%20CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)](https://tailwindcss.com/)
* [![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
* [![Azure Blob Storage](https://img.shields.io/badge/Azure%20Blob%20Storage-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/services/storage/blobs/)
* [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

