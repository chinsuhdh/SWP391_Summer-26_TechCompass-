import React from 'react';
import { Container, Row, Col, Form, Button } from 'react-bootstrap';
// Nhớ import các icon nếu bạn dùng react-icons, hoặc thay bằng thẻ <i> font-awesome
import { FaFacebookF, FaYoutube, FaLinkedinIn, FaTwitter } from 'react-icons/fa'; 

function Footer() {
  return (
    <footer   style={{ backgroundColor: '#0a0a14', color: 'rgba(255, 255, 255, 0.6)', padding: '5rem 0 2rem 0', fontSize: '0.9rem' }}>
      <Container >
        <Row className="gy-4">
          {/* Cột 1: Giới thiệu & Mạng xã hội */}
          <Col lg={4} md={12}>
            <div className="fw-bold text-white fs-4 mb-3">
              <span style={{ background: 'linear-gradient(to right, #00bfa5 0%, #00bfa5 30%, #ffffff 70%, #ffffff 100%)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', display: 'inline-block', fontWeight: '900', fontSize: '1.5rem' }}>
                AICareer
              </span>
            </div>
            <p style={{ maxWidth: '320px', lineHeight: '1.6', fontSize: '0.85rem' }}>
              Nền tảng AI học tập & nghề nghiệp cho sinh viên CNTT Việt Nam, thu hẹp khoảng cách giữa đại học và thị trường.
            </p>
            <div className="d-flex gap-2 mt-4">
              {[
                { icon: <FaFacebookF />, url: "#" },
                { icon: <FaYoutube />, url: "#" },
                { icon: <FaLinkedinIn />, url: "#" },
                { icon: <FaTwitter />, url: "#" }
              ].map((item, index) => (
                <a key={index} href={item.url} className="d-flex align-items-center justify-content-center text-white text-decoration-none" style={{ width: '36px', height: '36px', borderRadius: '8px', backgroundColor: 'rgba(255, 255, 255, 0.05)', border: '1px solid rgba(255, 255, 255, 0.08)', fontSize: '0.85rem', transition: 'all 0.2s' }}>
                  {item.icon}
                </a>
              ))}
            </div>
          </Col>

          {/* Cột 2: Nền tảng */}
          <Col lg={2} md={4} sm={6} xs={6}>
            <h6 className="text-white fw-bold mb-3" style={{ fontSize: '0.95rem' }}>Nền tảng</h6>
            <ul className="list-unstyled d-flex flex-column gap-2" style={{ fontSize: '0.85rem' }}>
              <li><a href="#features" className="text-white text-decoration-none opacity-75">Tính năng</a></li>
              <li><a href="#how-it-works" className="text-white text-decoration-none opacity-75">Cách hoạt động</a></li>
              <li><a href="#pricing" className="text-white text-decoration-none opacity-75">Bảng giá</a></li>
              <li><a href="#roadmap" className="text-white text-decoration-none opacity-75">Roadmap sản phẩm</a></li>
              <li><a href="#changelog" className="text-white text-decoration-none opacity-75">Changelog</a></li>
            </ul>
          </Col>

          {/* Cột 3: Tài nguyên */}
          <Col lg={2} md={4} sm={6} xs={6}>
            <h6 className="text-white fw-bold mb-3" style={{ fontSize: '0.95rem' }}>Tài nguyên</h6>
            <ul className="list-unstyled d-flex flex-column gap-2" style={{ fontSize: '0.85rem' }}>
              <li><a href="#blog" className="text-white text-decoration-none opacity-75">Blog & Tài nguyên</a></li>
              <li><a href="#guide" className="text-white text-decoration-none opacity-75">Career Guide</a></li>
              <li><a href="#discord" className="text-white text-decoration-none opacity-75">Cộng đồng Discord</a></li>
              <li><a href="#docs" className="text-white text-decoration-none opacity-75">API Documentation</a></li>
            </ul>
          </Col>

          {/* Cột 4: Công ty & Nhận tin */}
          <Col lg={4} md={4} sm={12}>
            <h6 className="text-white fw-bold mb-3" style={{ fontSize: '0.95rem' }}>Công ty</h6>
            <ul className="list-unstyled d-flex flex-column gap-2 mb-4" style={{ fontSize: '0.85rem' }}>
              <li><a href="#about" className="text-white text-decoration-none opacity-75">Về chúng tôi</a></li>
              <li><a href="#careers" className="text-white text-decoration-none opacity-75">Tuyển dụng</a></li>
              <li><a href="#privacy" className="text-white text-decoration-none opacity-75">Chính sách bảo mật</a></li>
              <li><a href="#terms" className="text-white text-decoration-none opacity-75">Điều khoản dịch vụ</a></li>
            </ul>
            
            <div style={{ maxWidth: '300px' }}>
              <label className="text-white opacity-75 mb-2" style={{ fontSize: '0.85rem' }}>Nhận cập nhật mới nhất</label>
              <div className="d-flex p-1" style={{ backgroundColor: 'rgba(255, 255, 255, 0.03)', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: '10px' }}>
                <Form.Control type="email" placeholder="Email của bạn" className="bg-transparent border-0 text-white shadow-none" style={{ fontSize: '0.85rem' }} />
                <Button className="d-flex align-items-center justify-content-center border-0" style={{ backgroundColor: '#00bfa5', color: '#0a0a14', borderRadius: '8px', padding: '0.5rem 0.75rem' }}>
                  →
                </Button>
              </div>
            </div>
          </Col>
        </Row>

        {/* Thanh bản quyền phía dưới */}
        <hr className="my-5" style={{ borderColor: 'rgba(255, 255, 255, 0.05)' }} />
        
        <div className="d-flex flex-column flex-md-row justify-content-between align-items-center gap-3" style={{ fontSize: '0.8rem', opacity: 0.5 }}>
          <div>© 2024 AICareer. Bảo lưu mọi quyền.</div>
          <div>Made with 💚 for SE Students in Vietnam</div>
        </div>

        {/* Chữ AICareer chìm cực lớn ở góc dưới bên trái */}
        <div className="user-select-none position-relative" style={{ height: '50px', overflow: 'hidden', marginTop: '2rem' }}>
          <span style={{ position: 'absolute', left: '-10px', bottom: '-40px', fontSize: '6.5rem', fontWeight: '900', color: 'rgba(255, 255, 255, 0.02)', letterSpacing: '-2px' }}>
            AICareer
          </span>
        </div>
      </Container>
    </footer>
  );
}

export default Footer;
