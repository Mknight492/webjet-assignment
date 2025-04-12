import React from "react";
import { useParams } from "react-router-dom";

const MovieDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  return (
    <div>
      <h1 className="text-3xl font-bold mb-6">Movie Details</h1>
      <div className="bg-white rounded-lg shadow p-6">
        <p>Details for movie ID: {id} will be implemented here</p>
      </div>
    </div>
  );
};

export default MovieDetailsPage; 