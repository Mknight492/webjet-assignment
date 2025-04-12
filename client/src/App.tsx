import { QueryProvider } from "./context/QueryProvider";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";

// You'll need to install react-router-dom with:
// npm install react-router-dom @types/react-router-dom

function App() {
  return (
    <QueryProvider>
      <Router>
        <div className="min-h-screen">
          <header className="bg-blue-600 text-white p-4">
            <h1 className="text-2xl font-bold">Movie Price Comparison</h1>
          </header>

          <main className="container mx-auto p-4">
            <Routes>
              <Route
                path="/"
                element={<div>Home Page - Will be implemented later</div>}
              />
              {/* Add more routes here */}
            </Routes>
          </main>

          <footer className="bg-gray-100 p-4 text-center text-gray-600">
            Movie Price Comparison App
          </footer>
        </div>
      </Router>
    </QueryProvider>
  );
}

export default App;
