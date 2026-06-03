import React from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';

const Register = () => {
    const leftBgImage = 'https://i.pinimg.com/736x/2a/2a/33/2a2a337f02b6c63548fb8e03b24a796a.jpg';

    return (
        <div className="container-fluid min-vh-100 p-0 d-flex font-sans" style={{ backgroundColor: '#0d1117' }}>
            <div className="row g-0 w-100">

                {/* PHẦN BÊN TRÁI: BANNER & THÔNG TIN AI */}
                
                <div className="col-lg-6 d-none d-lg-flex flex-column justify-content-between p-5 position-relative border-end border-secondary border-opacity-25"
                    style={{
                        backgroundImage: `linear-gradient(rgba(10, 25, 20, 0.88), rgba(10, 20, 20, 0.92)), url(${leftBgImage})`,
                        backgroundSize: 'cover',
                        backgroundPosition: 'center',
                        }}>

                    {/* Logo */}
                    <div className="fs-5 fw-bold text-white d-flex align-items-center gap-2">
                        <span style={{
                            background: 'linear-gradient(to right, #00bfa5 0%, #00bfa5 30%, #ffffff 70%, #ffffff 100%)',
                            WebkitBackgroundClip: 'text',
                            WebkitTextFillColor: 'transparent',
                            display: 'inline-block',
                            fontWeight: '900',
                            fontSize: '1.25rem'

                        }}>
                        AICareer
                        </span>
                    </div>

                    {/* Khối Widget AI & Đánh giá */}
                    <div className="my-auto mx-auto w-75 py-4" style={{ maxWidth: '400px' }}>

                        {/* Widget 1: AI đang phân tích */}
                        <div className="p-4 mb-3 rounded-4"
                            style={{ backgroundColor: 'rgba(255, 255, 255, 0.04)', border: '1px solid rgba(255, 255, 255, 0.08)', backdropFilter: 'blur(10px)' }}>
                            <div className="d-flex align-items-center mb-3 text-emerald" style={{ color: '#10b981', fontSize: '14px', fontWeight: '500' }}>
                                <i className="bi bi-cpu me-2"></i> AI đang phân tích
                            </div>

                            <div className="d-flex flex-column gap-3 text-white-50" style={{ fontSize: '0.75rem' }}>
                                {/* Kỹ năng 1 */}
                                <div>
                                    <div className="d-flex justify-content-between mb-1">
                                        <span>Kỹ năng Frontend</span>
                                        <span className="fw-bold" style={{ color: '#00bfa5' }}>87%</span>
                                    </div>
                                    <div className="progress" style={{ height: '6px', backgroundColor: '#30363d' }}>
                                        <div className="progress-bar" role="progressbar" style={{ width: '87%', backgroundColor: '#00bfa5' }}></div>
                                    </div>
                                </div>
                                {/* Kỹ năng 2 */}
                                <div>
                                    <div className="d-flex justify-content-between mb-1">
                                        <span>System Design</span>
                                        <span className="fw-bold text-warning">47%</span>
                                    </div>
                                    <div className="progress" style={{ height: '6px', backgroundColor: '#30363d' }}>
                                        <div className="progress-bar bg-warning" role="progressbar" style={{ width: '47%' }}></div>
                                    </div>
                                </div>
                                {/* Kỹ năng 3 */}
                                <div>
                                    <div className="d-flex justify-content-between mb-1">
                                        <span>DevOps & CI/CD</span>
                                        <span className="fw-bold text-danger">11%</span>
                                    </div>
                                    <div className="progress" style={{ height: '6px', backgroundColor: '#30363d' }}>
                                        <div className="progress-bar bg-danger" role="progressbar" style={{ width: '11%' }}></div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Widget 2: Thông báo thành công */}
                        <div className="p-4 mb-3 rounded-4"
                            style={{ backgroundColor: 'rgba(255, 255, 255, 0.04)', border: '1px solid rgba(255, 255, 255, 0.08)', backdropFilter: 'blur(10px)' }}>
                            <div className="rounded-circle d-flex align-items-center justify-content-center text-warning"
                                style={{ width: '32px', height: '32px', backgroundColor: 'rgba(255, 193, 7, 0.1)' }}>
                                <i className="bi bi-lightbulb-fill"></i>
                            </div>
                            <div>
                                <p className="m-0 fw-bold text-white" style={{ fontSize: '0.75rem' }}>Minh Tú vừa pass VNG!</p>
                                <p className="m-0 text-white-50" style={{ fontSize: '0.7rem' }}>Sau 3 tháng luyện theo roadmap AI</p>
                            </div>
                        </div>

                    </div>

                    {/* Testimonial phía dưới */}
                    <div className="mx-auto w-75" style={{ maxWidth: '400px' }}>
                        <p className="text-white-50 fst-italic fw-light lh-base mb-2" style={{ fontSize: '0.85rem' }}>
                            "Tôi không ngờ một bài test 15 phút lại lộ ra đúng điểm yếu mà tôi né tránh suốt 2 năm học."
                        </p>
                        <div style={{ fontSize: '0.75rem' }}>
                            <span className="text-white fw-medium">— Phương Anh</span>{" "}
                            <span className="text-secondary">- Frontend Dev tại Tiki</span>
                        </div>
                    </div>
                </div>

                {/* PHẦN BÊN PHẢI: FORM ĐĂNG KÝ */}
                <div className="col-10 col-sm-8 col-md-6 col-lg-6 mx-auto d-flex align-items-center justify-content-center p-4 p-md-5 bg-white text-dark min-vh-100">
                    <div className="w-100" style={{ maxWidth: '400px' }}>

                        {/* Tiêu đề */}
                        <div className="text-center text-lg-start mb-4">
                            <h2 className="fw-bold tracking-tight text-dark mb-1" style={{ fontSize: '1.6rem' }}>Tạo tài khoản mới</h2>
                            <p className="text-muted small mb-0">
                                Đã có tài khoản?{" "}
                                <a href="/login" className="fw-medium text-decoration-none" style={{ color: '#00bfa5' }}>
                                    Đăng nhập ngay
                                </a>
                            </p>
                        </div>

                        {/* Nút Đăng ký với Google */}
                        <button className="btn btn-outline-secondary w-100 d-flex align-items-center justify-content-center gap-2 py-2 mb-4 text-dark bg-transparent border-secondary border-opacity-25" style={{ fontSize: '0.85rem' }}>
                            <svg className="bi" width="16" height="16" viewBox="0 0 24 24">
                                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
                                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
                            </svg>
                            Tiếp tục với Google
                        </button>

                        {/* Đường phân cách */}
                        <div className="position-relative d-flex align-items-center justify-content-center mb-4">
                            <div className="position-absolute w-100 border-top border-secondary border-opacity-25"></div>
                            <span className="position-relative px-3 bg-white text-muted text-uppercase" style={{ fontSize: '0.65rem', letterSpacing: '0.05rem' }}>
                                hoặc đăng ký bằng email
                            </span>
                        </div>

                        {/* Form nhập liệu */}
                        <form className="d-flex flex-column gap-3">
                            {/* Họ và tên */}
                            <div>
                                <label className="form-label fw-bold text-secondary mb-1" style={{ fontSize: '0.75rem' }}>Họ và tên</label>
                                <div className="input-group">
                                    <span className="input-group-text bg-light border-end-0 text-muted"><i className="bi bi-person"></i></span>
                                    <input
                                        type="text"
                                        placeholder="Nguyễn Văn A"
                                        className="form-control form-control-sm bg-light border-start-0"
                                        style={{ fontSize: '0.85rem' }}
                                        required
                                    />
                                </div>
                            </div>

                            {/* Email */}
                            <div>
                                <label className="form-label fw-bold text-secondary mb-1" style={{ fontSize: '0.75rem' }}>Email</label>
                                <div className="input-group">
                                    <span className="input-group-text bg-light border-end-0 text-muted"><i className="bi bi-envelope"></i></span>
                                    <input
                                        type="email"
                                        placeholder="ten@email.com"
                                        className="form-control form-control-sm bg-light border-start-0"
                                        style={{ fontSize: '0.85rem' }}
                                        required
                                    />
                                </div>
                            </div>

                            {/* Mật khẩu */}
                            <div>
                                <label className="form-label fw-bold text-secondary mb-1" style={{ fontSize: '0.75rem' }}>Mật khẩu</label>
                                <div className="input-group">
                                    <span className="input-group-text bg-light border-end-0 text-muted"><i className="bi bi-lock"></i></span>
                                    <input
                                        type="password"
                                        placeholder="••••••••"
                                        className="form-control form-control-sm bg-light border-start-0"
                                        style={{ fontSize: '0.85rem' }}
                                        required
                                    />
                                </div>
                            </div>

                            {/* Điều khoản */}
                            <div className="form-check d-flex gap-1 align-items-start pt-1 m-0">
                                <input
                                    type="checkbox"
                                    id="terms"
                                    className="form-check-input mt-1 shadow-none"
                                    style={{ cursor: 'pointer' }}
                                    required
                                />
                                <label htmlFor="terms" className="form-check-label text-muted lh-sm" style={{ fontSize: '0.75rem', cursor: 'pointer' }}>
                                    Tôi đồng ý với{" "}
                                    <a href="/terms" className="text-decoration-none" style={{ color: '#00bfa5' }}>Điều khoản dịch vụ</a>
                                    {" "}và{" "}
                                    <a href="/privacy" className="text-decoration-none" style={{ color: '#00bfa5' }}>Chính sách bảo mật</a>.
                                </label>
                            </div>

                            {/* Nút submit */}
                            <button
                                type="submit"
                                className="btn border-0 w-100 d-flex align-items-center justify-content-center gap-1 py-2 text-white fw-medium shadow-sm mt-2"
                                style={{ backgroundColor: '#00bfa5', fontSize: '0.85rem' }}
                                onMouseOver={(e) => e.target.style.backgroundColor = '#00a68f'}
                                onMouseOut={(e) => e.target.style.backgroundColor = '#00bfa5'}
                            >
                                Đăng ký tài khoản
                                <i className="bi bi-arrow-right fs-6"></i>
                            </button>
                        </form>

                        {/* Footer chân trang */}
                        <p className="text-center text-muted mt-4 mb-0" style={{ fontSize: '0.65rem' }}>
                            Hệ thống được phát triển và bảo mật bởi AICareer.
                        </p>

                    </div>
                </div>

            </div>
        </div>
    );
};

export default Register;
