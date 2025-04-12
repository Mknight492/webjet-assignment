import React from "react";

const Footer: React.FC = () => {
  return (
    <footer className="bg-gray-100 py-6 mt-auto">
      <div className="container mx-auto px-4">
        <div className="text-center text-gray-600">
          <p className="mb-2">Movie Price Comparison App</p>
          <p className="text-sm">
            Comparing prices between Cinemaworld and Filmworld
          </p>
          <p className="text-xs mt-4">
            &copy; {new Date().getFullYear()} Movie Price Comparison
          </p>
        </div>
      </div>
    </footer>
  );
};

export default Footer; 