# **Movies and Actors Database Management**

## **Project Description**
This project is a web application for managing movies and actors with a many-to-many relationship. It allows users to:
- Add, edit, or delete movies and actors.
- Dynamically manage the relationships between movies and their actors.

The application uses **Blazor** for the frontend and **MongoDB** for the database.

---

## **Features**

### **Actors**
- Create, edit, and delete actors.
- Associate actors with one or more movies.

### **Movies**
- Create, edit, and delete movies.
- Associate movies with one or more actors.

### **Database**
- MongoDB is used to store data about movies and actors.
- Many-to-Many relationships are implemented between movies and actors.

---

## **Screenshots**

### Actor Management Interface:
[Actors Interface](https://imgur.com/a/aVjfEYk)

### Movie Management Interface:
[Movies Interface](https://imgur.com/a/A9px7KD)

---

## **Database Structure**

### **Collections**

1. **Actors**
   - Stores actor details and their associated movies.
   - Example:
     ```json
     {
       "_id": "ObjectId('673680fb5e767d07c2e2ec65')",
       "name": "Mark Hammil",
       "movies": [
         "ObjectId('673680e95e767d07c2e2ec63')"
       ]
     }
     ```

2. **Movies**
   - Stores movie details and their associated actors.
   - Example:
     ```json
     {
       "_id": "ObjectId('673680e95e767d07c2e2ec63')",
       "title": "Star Wars",
       "actors": [
         "ObjectId('673680fb5e767d07c2e2ec65')"
       ],
       "ActorsInfo": [
         {
           "_id": "ObjectId('673680fb5e767d07c2e2ec65')",
           "name": "Mark Hammil",
           "movies": ["ObjectId('673680e95e767d07c2e2ec63')"]
         }
       ]
     }
     ```

### **Many-to-Many Relationship**
- **Actors** can belong to multiple movies.
- **Movies** can have multiple actors.
- This relationship is achieved using reference IDs stored in each collection.

---

## **Installation**

### 1. Clone the Repository
```bash
git clone https://github.com/Victor19941221/Labb4
```

### 2. Install Dependencies
Navigate to the project directory and restore all required dependencies:
```bash
dotnet restore
```

### 3. Start MongoDB
Ensure MongoDB is running locally:
```bash
mongod
```

### 4. Seed the Database
To ensure the project works correctly, the following collections need to be created in MongoDB:

#### Actors Collection:
```json
[
  {
    "_id": "ObjectId('673680fb5e767d07c2e2ec65')",
    "name": "Mark Hammil",
    "movies": ["ObjectId('673680e95e767d07c2e2ec63')"]
  },
  {
    "_id": "ObjectId('673681145e767d07c2e2ec66')",
    "name": "Orlando Bloom",
    "movies": [
      "ObjectId('673680f25e767d07c2e2ec64')",
      "ObjectId('6736812b5e767d07c2e2ec67')"
    ]
  }
]
```

#### Movies Collection:
```json
[
  {
    "_id": "ObjectId('673680e95e767d07c2e2ec63')",
    "title": "Star Wars",
    "actors": ["ObjectId('673680fb5e767d07c2e2ec65')"],
    "ActorsInfo": [
      {
        "_id": "ObjectId('673680fb5e767d07c2e2ec65')",
        "name": "Mark Hammil",
        "movies": ["ObjectId('673680e95e767d07c2e2ec63')"]
      }
    ]
  },
  {
    "_id": "ObjectId('673680f25e767d07c2e2ec64')",
    "title": "Lord of the Rings",
    "actors": ["ObjectId('673681145e767d07c2e2ec66')"]
  }
]
```

### 5. Run the Project
Start the application using the following command:
```bash
dotnet run
```

---

## **Technologies Used**
- **Frontend**: Blazor (Interactive Server Components)
- **Backend**: C# .NET
- **Database**: MongoDB
- **ORM**: MongoDB.Driver

---

