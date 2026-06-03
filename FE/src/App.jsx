import React from "react";
import MyNavbar from "./components/MyNavbar";
import "bootstrap/dist/css/bootstrap.min.css";
import { Route, Routes, useLocation } from "react-router-dom";
import Home from "./pages/Home";
import 'bootstrap-icons/font/bootstrap-icons.css';
import Login from "./pages/Login"; 
import Register from "./pages/Register";

function App() {
  // Giờ đây useLocation() sẽ hoạt động bình thường vì App đã nằm trong <BrowserRouter> ở main.jsx
  const location = useLocation();

  const noNavarPaths = ['/login', '/register'];
  const showNavbar = !noNavarPaths.includes(location.pathname);

  return (
    <div className="position-relative" style={{ backgroundColor: '#0a0a14' }}>
      {showNavbar && <MyNavbar />}
      
      <Routes>
        <Route index element={<Home />} />
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Routes>
    </div>
  );
}

export default App;