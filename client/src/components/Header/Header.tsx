import React from "react";
import { Link } from "react-router-dom";

const Header: React.FC = () => {
  return (
    <header className="bg-blue-600 text-white shadow-md">
      <div className="container mx-auto px-4 py-3 flex justify-between items-center">
        <Link to="/" className="text-2xl font-bold flex items-center">
          <span>Movie Price Comparison</span>
        </Link>
        
        <nav className="hidden md:flex space-x-6">
          <Link to="/" className="hover:text-blue-200 transition-colors">
            Home
          </Link>
        </nav>
      </div>
    </header>
  );
};

export default Header; 