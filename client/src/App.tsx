import { QueryProvider } from "./context/QueryProvider";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Header from "./components/Header";
import Footer from "./components/Footer";
import MoviesPage from "./pages/MoviesPage";

// You'll need to install react-router-dom with:
// npm install react-router-dom @types/react-router-dom

function App() {
  return (
    <QueryProvider>
      <Router>
        <div className="flex flex-col min-h-screen">
          <Header />
          
          <main className="container mx-auto px-4 py-6 flex-grow">
            <Routes>
              <Route path="/" element={<MoviesPage />} />
              {/* Add more routes as needed */}
            </Routes>
          </main>
          
          <Footer />
        </div>
      </Router>
    </QueryProvider>
  );
}

export default App;
